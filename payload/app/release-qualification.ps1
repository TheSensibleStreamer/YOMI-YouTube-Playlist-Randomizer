param(
    [string]$Root = '',
    [ValidateSet('PreSeal','Sealed','Installed')][string]$Mode = 'Installed',
    [string]$ContractPath = '',
    [switch]$NoReport
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0

if([string]::IsNullOrWhiteSpace($Root)){$Root=Split-Path $PSScriptRoot -Parent}
$Root=[IO.Path]::GetFullPath($Root).TrimEnd('\')
$app=Join-Path $Root 'app'
if([string]::IsNullOrWhiteSpace($ContractPath)){$ContractPath=Join-Path $app 'YOMI-CAPABILITY-CONTRACT.json'}

function Sha256([string]$Path){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Prop($Object,[string]$Name){if($null -eq $Object){return $null};$p=$Object.PSObject.Properties[$Name];if($null -eq $p){return $null};return $p.Value}
$results=New-Object System.Collections.Generic.List[object]
function Add-Result([string]$Id,[string]$Status,[string]$Detail){$results.Add([pscustomobject]@{id=$Id;status=$Status;detail=$Detail})}
function Fail([string]$Id,[string]$Detail){Add-Result $Id 'FAIL' $Detail}
function Pass([string]$Id,[string]$Detail){Add-Result $Id 'PASS' $Detail}
function Invoke-QualificationSelfTest([string]$Exe,[string]$WorkingDirectory,[int]$TimeoutMs=45000){
    $diag=Join-Path $env:TEMP ('YOMI-WPF-SELFTEST-'+[Guid]::NewGuid().ToString('N')+'.txt')
    $oldDiag=$env:YOMI_SELFTEST_DIAGNOSTIC;$hadDiag=Test-Path Env:\YOMI_SELFTEST_DIAGNOSTIC;$p=$null
    try{$env:YOMI_SELFTEST_DIAGNOSTIC=$diag;$p=Start-Process -FilePath $Exe -ArgumentList '--self-test' -WorkingDirectory $WorkingDirectory -PassThru}
    finally{if($hadDiag){$env:YOMI_SELFTEST_DIAGNOSTIC=$oldDiag}else{Remove-Item Env:\YOMI_SELFTEST_DIAGNOSTIC -ErrorAction SilentlyContinue}}
    try{
        if(-not $p.WaitForExit($TimeoutMs)){try{$p.Kill()}catch{};try{$p.WaitForExit()}catch{};return [pscustomobject]@{exit=124;detail='timeout after '+$TimeoutMs+' ms'}}
        $detail='';if(Test-Path -LiteralPath $diag){try{$detail=(Get-Content -LiteralPath $diag -Raw -Encoding UTF8).Trim()}catch{}}
        return [pscustomobject]@{exit=[int]$p.ExitCode;detail=$detail}
    }finally{if($null -ne $p){$p.Dispose()};Remove-Item -LiteralPath $diag -Force -ErrorAction SilentlyContinue}
}


if(-not(Test-Path -LiteralPath $ContractPath -PathType Leaf)){throw "Capability contract missing: $ContractPath"}
$contract=Get-Content -LiteralPath $ContractPath -Raw -Encoding UTF8|ConvertFrom-Json
if([string](Prop $contract 'product') -notlike 'YOMI*'){throw 'Capability contract product identity is invalid.'}
if([string](Prop $contract 'version') -ne '4.2.0.8'){throw 'Capability contract version mismatch.'}
if([string](Prop $contract 'qualification') -ne 'DEV13.59'){throw 'Capability contract qualification identity mismatch.'}

$versionPath=Join-Path $Root 'VERSION.txt'
if(Test-Path -LiteralPath $versionPath){$v=(Get-Content -LiteralPath $versionPath -Raw).Trim();if($v -eq '4.2.0.8'){Pass 'IDENTITY.VERSION' $v}else{Fail 'IDENTITY.VERSION' ("expected 4.2.0.8, found "+$v)}}else{Fail 'IDENTITY.VERSION' 'VERSION.txt missing'}

foreach($relObj in @($contract.required_files)){
    $rel=[string]$relObj
    $path=Join-Path $Root ($rel.Replace('/','\'))
    if(Test-Path -LiteralPath $path -PathType Leaf){Pass ('FILE.'+$rel) 'present'}else{Fail ('FILE.'+$rel) 'missing'}
}

foreach($cap in @($contract.capabilities)){
    $capId=[string]$cap.id
    $capName=[string]$cap.name
    $capOk=$true
    $messages=New-Object System.Collections.Generic.List[string]
    foreach($probe in @($cap.probes)){
        $rel=[string]$probe.file
        $path=Join-Path $Root ($rel.Replace('/','\'))
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)){$capOk=$false;$messages.Add($rel+':missing');continue}
        try{$text=Get-Content -LiteralPath $path -Raw -Encoding UTF8}catch{$capOk=$false;$messages.Add($rel+':unreadable');continue}
        $all=Prop $probe 'contains_all'
        if($null -ne $all){
            foreach($needleObj in @($all)){$needle=[string]$needleObj;if($text.IndexOf($needle,[StringComparison]::Ordinal) -lt 0){$capOk=$false;$messages.Add($rel+':missing '+$needle)}}
        }
        $any=Prop $probe 'contains_any'
        if($null -ne $any){
            $found=$false
            foreach($needleObj in @($any)){$needle=[string]$needleObj;if($text.IndexOf($needle,[StringComparison]::Ordinal) -ge 0){$found=$true;break}}
            if(-not $found){$capOk=$false;$messages.Add($rel+':none-of-any-signatures')}
        }
    }
    if($capOk){Pass ('CAP.'+$capId) $capName}else{Fail ('CAP.'+$capId) ($capName+' :: '+($messages -join ' | '))}
}

$ledgerPath=Join-Path $app 'NO-GAPS-CONSOLIDATION-LEDGER.json'
try{
    $ledger=Get-Content -LiteralPath $ledgerPath -Raw -Encoding UTF8|ConvertFrom-Json
    if([string]$ledger.status -eq 'NO_UNEXPLAINED_GAPS' -and @($ledger.known_gaps).Count -eq 0 -and [string]$ledger.consolidation -eq 'DEV13.59'){Pass 'LINEAGE.NO_GAPS' 'DEV13.59 / zero known gaps'}else{Fail 'LINEAGE.NO_GAPS' 'ledger status, head, or gap count is wrong'}
}catch{Fail 'LINEAGE.NO_GAPS' $_.Exception.Message}

$registryPath=Join-Path $app 'EXPEDITION-MODULE-REGISTRY.json'
try{
    $reg=Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8|ConvertFrom-Json
    if([int]$reg.source_module_count -eq 47){Pass 'EXPEDITION.RECOVERED_MODULES' '47'}else{Fail 'EXPEDITION.RECOVERED_MODULES' ([string]$reg.source_module_count)}
    if([int]$reg.workspace.route_count -eq 28){Pass 'EXPEDITION.WORKSPACE_ROUTES' '28'}else{Fail 'EXPEDITION.WORKSPACE_ROUTES' ([string]$reg.workspace.route_count)}
    if([int]$reg.world_passport_spine.object_cap -eq 2048 -and [int]$reg.world_passport_spine.relation_cap -eq 8192){Pass 'EXPEDITION.PASSPORT_CAPS' '2048 objects / 8192 relations'}else{Fail 'EXPEDITION.PASSPORT_CAPS' 'registry cap mismatch'}
}catch{Fail 'EXPEDITION.REGISTRY' $_.Exception.Message}

try{
    $expDocs=Join-Path $Root 'docs\expedition';$docCount=if(Test-Path -LiteralPath $expDocs){@(Get-ChildItem -LiteralPath $expDocs -File).Count}else{0}
    if($docCount -ge 102){Pass 'LINEAGE.EXPEDITION_EVIDENCE' ([string]$docCount)}else{Fail 'LINEAGE.EXPEDITION_EVIDENCE' ('expected at least 102, found '+$docCount)}
}catch{Fail 'LINEAGE.EXPEDITION_EVIDENCE' $_.Exception.Message}

try{
    $mods=@(Get-ChildItem -LiteralPath $app -Filter 'Yomi*.cs' -File|Where-Object{$_.Name -notin @('YomiControllerWpf.cs','YomiLauncher.cs')})
    if($mods.Count -eq 49){Pass 'BUILD.SUPPORT_MODULES' '49'}else{Fail 'BUILD.SUPPORT_MODULES' ("found "+$mods.Count)}
}catch{Fail 'BUILD.SUPPORT_MODULES' $_.Exception.Message}

try{
    $parseFailures=New-Object System.Collections.Generic.List[string]
    foreach($ps in @(Get-ChildItem -LiteralPath $app -Filter '*.ps1' -File)){
        $tokens=$null;$errors=$null
        [System.Management.Automation.Language.Parser]::ParseFile($ps.FullName,[ref]$tokens,[ref]$errors)|Out-Null
        if($errors -and $errors.Count -gt 0){$parseFailures.Add($ps.Name+': '+$errors[0].Message)}
    }
    if($parseFailures.Count -eq 0){Pass 'BUILD.POWERSHELL_PARSE' 'all app PowerShell scripts parse'}else{Fail 'BUILD.POWERSHELL_PARSE' ($parseFailures -join ' | ')}
}catch{Fail 'BUILD.POWERSHELL_PARSE' $_.Exception.Message}

try{
    $exe=Join-Path $app 'YomiControllerWpf.exe'
    if(Test-Path -LiteralPath $exe){$probe=Invoke-QualificationSelfTest -Exe $exe -WorkingDirectory $app;if([int]$probe.exit -eq 0){Pass 'BUILD.WPF_SELF_TEST' 'exit 0'}elseif([string]::IsNullOrWhiteSpace([string]$probe.detail)){Fail 'BUILD.WPF_SELF_TEST' ('exit '+$probe.exit)}else{Fail 'BUILD.WPF_SELF_TEST' ('exit '+$probe.exit+' :: '+$probe.detail)}}else{Fail 'BUILD.WPF_SELF_TEST' 'controller executable missing'}
}catch{Fail 'BUILD.WPF_SELF_TEST' $_.Exception.Message}

if($Mode -ne 'PreSeal'){
    $receiptPath=Join-Path $app 'INSTALLATION-RECEIPT.json'
    try{
        $receipt=Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8|ConvertFrom-Json
        $seen=@{}
        foreach($p in @($receipt.patches)){if([string]$p.status -eq 'PASS'){$seen[[string]$p.id]=$true}}
        $missing=New-Object System.Collections.Generic.List[string]
        foreach($idObj in @($contract.required_patch_ids)){$id=[string]$idObj;if(-not $seen.ContainsKey($id)){$missing.Add($id)}}
        if($missing.Count -eq 0 -and [string]$receipt.status -eq 'SEALED'){Pass 'RELEASE.PATCH_RECEIPT' ("all "+@($contract.required_patch_ids).Count+' required patches recorded')}else{Fail 'RELEASE.PATCH_RECEIPT' ('missing='+($missing -join ',')+' status='+[string]$receipt.status)}
    }catch{Fail 'RELEASE.PATCH_RECEIPT' $_.Exception.Message}

    $integrityPath=Join-Path $app 'RUNTIME-INTEGRITY-MANIFEST.json'
    try{
        $integrity=Get-Content -LiteralPath $integrityPath -Raw -Encoding UTF8|ConvertFrom-Json
        $bad=New-Object System.Collections.Generic.List[string]
        foreach($f in @($integrity.files)){
            $rel=[string]$f.path;$path=Join-Path $Root ($rel.Replace('/','\'))
            if(-not(Test-Path -LiteralPath $path -PathType Leaf)){$bad.Add($rel+':missing');continue}
            if((Get-Item -LiteralPath $path).Length -ne [int64]$f.bytes){$bad.Add($rel+':size');continue}
            if((Sha256 $path) -ne [string]$f.sha256){$bad.Add($rel+':sha256')}
        }
        if($bad.Count -eq 0 -and [string]$integrity.seal -eq 'DEV13.59'){Pass 'RELEASE.RUNTIME_INTEGRITY' ("verified "+@($integrity.files).Count+' immutable files')}else{Fail 'RELEASE.RUNTIME_INTEGRITY' ($bad -join ' | ')}
    }catch{Fail 'RELEASE.RUNTIME_INTEGRITY' $_.Exception.Message}
}

$failures=@($results|Where-Object{$_.status -eq 'FAIL'})
$passes=@($results|Where-Object{$_.status -eq 'PASS'})
$status=if($failures.Count -eq 0){'PASS'}else{'FAIL'}
$report=[ordered]@{
    schema=1;product='YOMI';version='4.2.0.8';qualification='DEV13.59';mode=$Mode;status=$status;
    generated_utc=[DateTime]::UtcNow.ToString('o');root=$Root;pass_count=$passes.Count;failure_count=$failures.Count;
    capability_count=@($contract.capabilities).Count;results=$results.ToArray()
}

if(-not $NoReport){
    try{
        $dataRoot=Join-Path $env:LOCALAPPDATA 'YOMI';$reportRoot=Join-Path $dataRoot 'reports';New-Item -ItemType Directory -Path $reportRoot -Force|Out-Null
        $stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
        $jsonPath=Join-Path $reportRoot ('YOMI-release-qualification-'+$stamp+'.json')
        $txtPath=Join-Path $reportRoot ('YOMI-release-qualification-'+$stamp+'.txt')
        [IO.File]::WriteAllText($jsonPath,($report|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
        $lines=New-Object System.Collections.Generic.List[string]
        $lines.Add('===== YOMI DEV13.59 RELEASE QUALIFICATION =====');$lines.Add('STATUS: '+$status);$lines.Add('MODE: '+$Mode);$lines.Add('PASS: '+$passes.Count+'  FAIL: '+$failures.Count);$lines.Add('')
        foreach($r in $results.ToArray()){$lines.Add(('{0,-6} {1} :: {2}' -f $r.status,$r.id,$r.detail))}
        [IO.File]::WriteAllLines($txtPath,$lines,[Text.UTF8Encoding]::new($false))
        $report['json_report']=$jsonPath;$report['text_report']=$txtPath
    }catch{}
}

Write-Host ''
Write-Host '===== YOMI DEV13.59 RELEASE QUALIFICATION =====' -ForegroundColor Cyan
Write-Host ('MODE: '+$Mode)
Write-Host ('CAPABILITIES: '+@($contract.capabilities).Count)
Write-Host ('PASS: '+$passes.Count+'  FAIL: '+$failures.Count) -ForegroundColor $(if($failures.Count -eq 0){'Green'}else{'Red'})
if($failures.Count -gt 0){foreach($r in $failures){Write-Host ('FAIL  '+$r.id+' :: '+$r.detail) -ForegroundColor Red}}
Write-Output ($report|ConvertTo-Json -Depth 12)
if($failures.Count -gt 0){exit 10}
exit 0
