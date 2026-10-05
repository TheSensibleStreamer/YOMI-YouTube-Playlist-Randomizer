param(
    [ValidateSet('Create','Verify','Restore','List')]
    [string]$Mode='Create',
    [string]$Path='',
    [string]$Name='',
    [switch]$IncludeHistory,
    [ValidateRange(0,200)]
    [int]$Retention=20,
    [switch]$Force
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$recoveryRoot=Join-Path $DataRoot 'recovery-points'
New-Item -ItemType Directory -Path $recoveryRoot -Force|Out-Null

function Write-NoBom([string]$Target,[string]$Text){
    $parent=Split-Path $Target -Parent
    if($parent -and -not(Test-Path $parent)){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
    [IO.File]::WriteAllText($Target,$Text,(New-Object System.Text.UTF8Encoding($false)))
}
function Sha256([string]$Target){return (Get-FileHash $Target -Algorithm SHA256).Hash.ToLowerInvariant()}
function Safe-Relative([string]$Relative){
    if([string]::IsNullOrWhiteSpace($Relative)){return $false}
    $n=$Relative.Replace('\','/')
    $parts=$n.Split('/')
    if([IO.Path]::IsPathRooted($Relative) -or $n.Contains(':') -or $parts -contains '..'){return $false}
    foreach($segment in $parts){
        if([string]::IsNullOrWhiteSpace($segment)){return $false}
        if($segment.EndsWith('.') -or $segment.EndsWith(' ')){return $false}
        if($segment -match '(?i)^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)'){return $false}
    }
    return $true
}
function Yomi-Running {
    foreach($pidName in @('supervisor.pid','engine.pid')){
        $pidPath=Join-Path $DataRoot ('state\'+$pidName)
        if(Test-Path $pidPath){
            $value=0
            try{[void][int]::TryParse((Get-Content $pidPath -Raw).Trim(),[ref]$value)}catch{}
            if($value -gt 0 -and (Get-Process -Id $value -ErrorAction SilentlyContinue)){return $true}
        }
    }
    return $false
}
function State-Files([bool]$WithHistory){
    $files=@(
        'config.json',
        'config.previous.json',
        'playlist.txt',
        'state/session.json',
        'state/session-order.json',
        'state/session-order.previous.json',
        'state/session-journal.jsonl',
        'state/session-journal-head.json',
        'state/resume-track.txt'
    )
    if($WithHistory){$files+='state/history.jsonl'}
    return $files
}
function Read-Json([string]$Target){
    return Get-Content $Target -Raw|ConvertFrom-Json
}
function Verify-RecoveryPoint([string]$ZipPath,[switch]$Extract,[string]$ExtractRoot=''){
    if(-not(Test-Path $ZipPath)){throw "Recovery point does not exist: $ZipPath"}
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive=[IO.Compression.ZipFile]::OpenRead($ZipPath)
    try{
        $seen=New-Object 'System.Collections.Generic.HashSet[System.String]' ([System.StringComparer]::OrdinalIgnoreCase)
        $entries=@{}
        [int64]$expanded=0
        foreach($entry in $archive.Entries){
            $name=([string]$entry.FullName).Replace('\','/')
            if(-not(Safe-Relative $name)){throw "Unsafe recovery-point path: $name"}
            if(-not $seen.Add($name)){throw "Duplicate recovery-point entry: $name"}
            $expanded += [int64]$entry.Length
            if($expanded -gt 512MB){throw 'Recovery point expands beyond the 512 MB safety ceiling.'}
            $entries[$name]=$entry
        }
        if(-not $entries.ContainsKey('manifest.json')){throw 'Recovery point manifest.json is missing.'}

        $reader=New-Object IO.StreamReader($entries['manifest.json'].Open(),[System.Text.Encoding]::UTF8,$true)
        try{$manifestText=$reader.ReadToEnd()}finally{$reader.Dispose()}
        $manifest=$manifestText|ConvertFrom-Json
        if([int]$manifest.schema -ne 1){throw "Unsupported recovery-point schema $($manifest.schema)."}
        if([string]$manifest.product -ne 'YOMI'){throw 'Recovery point product identity is not YOMI.'}

        foreach($file in @($manifest.files)){
            $rel=[string]$file.path
            if(-not(Safe-Relative $rel)){throw "Unsafe manifest file path: $rel"}
            if(-not $entries.ContainsKey($rel)){throw "Recovery point file missing: $rel"}
            $entry=$entries[$rel]
            if([int64]$file.bytes -ne [int64]$entry.Length){throw "Recovery point size mismatch: $rel"}
            $stream=$entry.Open()
            try{
                $sha=[Security.Cryptography.SHA256]::Create()
                try{$hash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}
                finally{$sha.Dispose()}
            }finally{$stream.Dispose()}
            if($hash -ne ([string]$file.sha256).ToLowerInvariant()){throw "Recovery point hash mismatch: $rel"}
        }

        if($manifest.contracts){
            $supported=Get-YomiContractSnapshot
            foreach($name in @('config_schema','session_schema','occurrence_order_schema')){
                if($null -ne $manifest.contracts.PSObject.Properties[$name] -and [int]$manifest.contracts.$name -gt [int]$supported.$name){
                    throw "Recovery point $name $($manifest.contracts.$name) is newer than this YOMI supports ($($supported.$name))."
                }
            }
        }

        if($Extract){
            if([string]::IsNullOrWhiteSpace($ExtractRoot)){throw 'ExtractRoot is required.'}
            New-Item -ItemType Directory -Path $ExtractRoot -Force|Out-Null
            foreach($file in @($manifest.files)){
                $rel=[string]$file.path
                $dest=Join-Path $ExtractRoot ($rel.Replace('/','\'))
                $parent=Split-Path $dest -Parent
                New-Item -ItemType Directory -Path $parent -Force|Out-Null
                $input=$entries[$rel].Open()
                try{
                    $output=New-Object IO.FileStream($dest,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
                    try{$input.CopyTo($output)}finally{$output.Dispose()}
                }finally{$input.Dispose()}
            }
        }
        return $manifest
    }finally{$archive.Dispose()}
}
function Create-RecoveryPoint([string]$RequestedName,[bool]$WithHistory){
    $stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
    $safeName=if([string]::IsNullOrWhiteSpace($RequestedName)){'checkpoint'}else{[regex]::Replace($RequestedName,'[^A-Za-z0-9_.-]+','-').Trim('-')}
    if([string]::IsNullOrWhiteSpace($safeName)){$safeName='checkpoint'}
    $zip=Join-Path $recoveryRoot ("YOMI-recovery-$stamp-$safeName.zip")
    $work=Join-Path ([IO.Path]::GetTempPath()) ('yomi-recovery-create-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work|Out-Null
    try{
        $relativeFiles=State-Files $WithHistory
        $stable=$false
        for($attempt=1;$attempt -le 4 -and -not $stable;$attempt++){
            Get-ChildItem $work -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            $before=@{}
            $beforeExists=@{}
            foreach($rel in $relativeFiles){
                $source=Join-Path $DataRoot ($rel.Replace('/','\'))
                $exists=Test-Path $source
                $beforeExists[$rel]=$exists
                if($exists){
                    try{$before[$rel]=[ordered]@{bytes=[int64](Get-Item $source).Length;sha256=Sha256 $source}}catch{$beforeExists[$rel]=$false}
                }
            }

            foreach($rel in @($before.Keys)){
                $source=Join-Path $DataRoot ($rel.Replace('/','\'))
                $dest=Join-Path $work ($rel.Replace('/','\'))
                New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force|Out-Null
                Copy-Item $source $dest -Force
            }

            $stable=$true
            foreach($rel in $relativeFiles){
                $source=Join-Path $DataRoot ($rel.Replace('/','\'))
                if((Test-Path $source) -ne [bool]$beforeExists[$rel]){$stable=$false;break}
            }
            foreach($rel in @($before.Keys)){
                if(-not $stable){break}
                $source=Join-Path $DataRoot ($rel.Replace('/','\'))
                $dest=Join-Path $work ($rel.Replace('/','\'))
                if(-not(Test-Path $source) -or -not(Test-Path $dest)){$stable=$false;break}
                try{
                    $sourceNow=Sha256 $source
                    $copyHash=Sha256 $dest
                    if($sourceNow -ne [string]$before[$rel].sha256 -or $copyHash -ne [string]$before[$rel].sha256){$stable=$false;break}
                }catch{$stable=$false;break}
            }
            if(-not $stable){Start-Sleep -Milliseconds (75*$attempt)}
        }
        if(-not $stable){throw 'Authoritative YOMI state changed repeatedly while the Recovery Point was being captured. Stop playback briefly or retry.'}

        $files=@()
        foreach($rel in @($before.Keys|Sort-Object)){
            $dest=Join-Path $work ($rel.Replace('/','\'))
            $files+=[ordered]@{path=$rel.Replace('\','/');bytes=[int64](Get-Item $dest).Length;sha256=Sha256 $dest}
        }

        $sessionId=''
        $sessionPath=Join-Path $work 'state\session.json'
        if(Test-Path $sessionPath){try{$sessionId=[string](Read-Json $sessionPath).session_id}catch{}}
        $manifest=[ordered]@{
            schema=1
            product='YOMI'
            created_utc=[DateTime]::UtcNow.ToString('o')
            app_version=Get-YomiVersionText
            session_id=$sessionId
            include_history=$WithHistory
            contracts=Get-YomiContractSnapshot
            files=$files
        }
        Write-NoBom (Join-Path $work 'manifest.json') ($manifest|ConvertTo-Json -Depth 8)
        Compress-Archive -Path (Join-Path $work '*') -DestinationPath $zip -CompressionLevel Optimal
        $verified=Verify-RecoveryPoint $zip
        if($Retention -gt 0){
            $points=@(Get-ChildItem $recoveryRoot -Filter 'YOMI-recovery-*.zip' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
            if($points.Count -gt $Retention){
                $points|Select-Object -Skip $Retention|Remove-Item -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Host "Recovery point created: $zip" -ForegroundColor Green
        Write-Host "Files: $(@($verified.files).Count)  SHA-256: $(Sha256 $zip)"
        try{Set-Clipboard $zip}catch{}
        return $zip
    }finally{Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue}
}
function Restore-RecoveryPoint([string]$ZipPath){
    if(Yomi-Running){throw 'YOMI is running. Stop playback before restoring a recovery point.'}
    $extract=Join-Path ([IO.Path]::GetTempPath()) ('yomi-recovery-restore-'+[Guid]::NewGuid().ToString('N'))
    $rollback=Join-Path ([IO.Path]::GetTempPath()) ('yomi-recovery-rollback-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $extract,$rollback|Out-Null
    try{
        $manifest=Verify-RecoveryPoint $ZipPath -Extract -ExtractRoot $extract

        $configPath=Join-Path $extract 'config.json'
        if(Test-Path $configPath){
            $restoreConfig=Read-Json $configPath
            $supported=Get-YomiContractSnapshot
            $schema=if($null -ne $restoreConfig.PSObject.Properties['config_schema_version']){[int]$restoreConfig.config_schema_version}else{1}
            if($schema -gt [int]$supported.config_schema){throw "Recovery config schema $schema is newer than supported schema $($supported.config_schema)."}
        }

        $sessionPath=Join-Path $extract 'state\session.json'
        $orderPath=Join-Path $extract 'state\session-order.json'
        if((Test-Path $sessionPath) -and (Test-Path $orderPath)){
            $session=Read-Json $sessionPath;$order=Read-Json $orderPath
            if([string]$session.session_id -ne [string]$order.session_id){throw 'Recovery session/order identities do not match.'}
        }

        if(-not $Force){
            $answer=[System.Windows.Forms.MessageBox]::Show(
                "Restore this YOMI recovery point?`r`n`r`nA pre-restore recovery point will be created first. Playback must remain stopped.",
                'YOMI Recovery Point',
                [System.Windows.Forms.MessageBoxButtons]::YesNo,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            )
            if($answer -ne [System.Windows.Forms.DialogResult]::Yes){Write-Host 'Restore cancelled.';return}
        }

        [void](Create-RecoveryPoint 'pre-restore' $true)

        $targets=@($manifest.files|ForEach-Object{[string]$_.path})
        foreach($rel in $targets){
            $current=Join-Path $DataRoot ($rel.Replace('/','\'))
            if(Test-Path $current){
                $backup=Join-Path $rollback ($rel.Replace('/','\'))
                New-Item -ItemType Directory -Path (Split-Path $backup -Parent) -Force|Out-Null
                Copy-Item $current $backup -Force
            }
        }

        $applied=New-Object System.Collections.Generic.List[string]
        try{
            foreach($rel in $targets){
                $source=Join-Path $extract ($rel.Replace('/','\'))
                $dest=Join-Path $DataRoot ($rel.Replace('/','\'))
                New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force|Out-Null
                $tmp=$dest+'.restore.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
                Copy-Item $source $tmp -Force
                if(Test-Path $dest){
                    $old=$dest+'.replace-old.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
                    Move-Item $dest $old -Force
                    try{Move-Item $tmp $dest -Force;Remove-Item $old -Force}
                    catch{
                        Remove-Item $tmp,$dest -Force -ErrorAction SilentlyContinue
                        if(Test-Path $old){Move-Item $old $dest -Force}
                        throw
                    }
                }else{Move-Item $tmp $dest -Force}
                $applied.Add($rel)
            }

            foreach($transient in @(
                'state/current.json','state/queue-runtime.json','state/engine-status.json',
                'state/runtime-instance.json','state/runtime-lease.json','state/watchdog-status.json',
                'state/browser-clients.json','state/rehearsal-report.json',
                'state/engine.pid','state/server.pid','state/supervisor.pid'
            )){Remove-Item (Join-Path $DataRoot $transient) -Force -ErrorAction SilentlyContinue}

            Write-Host "Recovery point restored successfully: $ZipPath" -ForegroundColor Green
        }catch{
            foreach($rel in $applied.ToArray()){
                $dest=Join-Path $DataRoot ($rel.Replace('/','\'))
                $backup=Join-Path $rollback ($rel.Replace('/','\'))
                Remove-Item $dest -Force -ErrorAction SilentlyContinue
                if(Test-Path $backup){
                    New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force|Out-Null
                    Copy-Item $backup $dest -Force
                }
            }
            throw
        }
    }finally{
        Remove-Item $extract,$rollback -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Add-Type -AssemblyName System.Windows.Forms
switch($Mode){
    'Create' {[void](Create-RecoveryPoint $Name ([bool]$IncludeHistory))}
    'Verify' {
        if([string]::IsNullOrWhiteSpace($Path)){throw '-Path is required for Verify.'}
        $m=Verify-RecoveryPoint $Path
        Write-Host "PASS recovery point: $Path" -ForegroundColor Green
        Write-Host ($m|ConvertTo-Json -Depth 5)
    }
    'Restore' {
        if([string]::IsNullOrWhiteSpace($Path)){
            $picker=New-Object System.Windows.Forms.OpenFileDialog
            $picker.Title='Restore YOMI Recovery Point'
            $picker.Filter='YOMI Recovery Point (*.zip)|*.zip|All files (*.*)|*.*'
            $picker.InitialDirectory=$recoveryRoot
            $picker.Multiselect=$false
            if($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){Write-Host 'Restore cancelled.';return}
            $Path=$picker.FileName
        }
        Restore-RecoveryPoint $Path
    }
    'List' {
        Get-ChildItem $recoveryRoot -Filter 'YOMI-recovery-*.zip' -File -ErrorAction SilentlyContinue|
            Sort-Object LastWriteTime -Descending|
            Select-Object LastWriteTime,Length,FullName|Format-Table -AutoSize
    }
}
