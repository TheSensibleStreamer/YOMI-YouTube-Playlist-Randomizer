param([Parameter(Mandatory=$true,Position=0)][string]$PackagePath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0
# R61.64 update-lane continuity: guarded revision sequencing plus installer AST safety prevents stale packages and automatic-variable collisions before elevation.

$DataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'
$Log=Join-Path $DataRoot 'update-host.log'
$TranscriptLog=Join-Path $DataRoot 'update-host-transcript.log'
$Work=Join-Path $env:TEMP ('YOMI-UPDATE-HOST-'+[Guid]::NewGuid().ToString('N'))
$Success=$false

function Show-YomiMessage([string]$Text,[string]$Caption,[bool]$Error){
    try{
        Add-Type -AssemblyName PresentationFramework -ErrorAction Stop
        $icon=if($Error){[System.Windows.MessageBoxImage]::Error}else{[System.Windows.MessageBoxImage]::Information}
        [void][System.Windows.MessageBox]::Show($Text,$Caption,[System.Windows.MessageBoxButton]::OK,$icon)
    }catch{}
}
function Normalize-EntryName([string]$Name){
    return (($Name -replace '\\','/').TrimStart('/'))
}
function Assert-SafeRelativePath([string]$Name){
    $n=Normalize-EntryName $Name
    if([string]::IsNullOrWhiteSpace($n)){throw 'Package contains an empty path.'}
    if($n.StartsWith('/') -or $n -match '^[A-Za-z]:' -or $n -match '(^|/)\.\.(/|$)'){throw ('Unsafe package path: '+$Name)}
    return $n
}
function Read-ZipEntryBytes($Entry){
    $stream=$Entry.Open()
    try{
        $memory=New-Object IO.MemoryStream
        try{$stream.CopyTo($memory);return $memory.ToArray()}finally{$memory.Dispose()}
    }finally{$stream.Dispose()}
}
function Sha256-Bytes([byte[]]$Bytes){
    $sha=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
function Write-Bytes([string]$Path,[byte[]]$Bytes){
    $parent=Split-Path -Parent $Path
    if($parent){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
    [IO.File]::WriteAllBytes($Path,$Bytes)
}
function Get-OptionalManifestText($Object,[string]$Name){
    if($null -eq $Object){return ''}
    $property=$Object.PSObject.Properties[$Name]
    if($null -eq $property -or $null -eq $property.Value){return ''}
    return [string]$property.Value
}
function Get-RevisionToken([string]$Text){
    if([string]::IsNullOrWhiteSpace($Text)){return ''}
    $match=[regex]::Match($Text,'R\d+\.\d+(?:\.\d+)?',[Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if($match.Success){return $match.Value.ToUpperInvariant()}
    return ''
}

function Assert-NoReservedAutomaticVariableAssignment($Ast){
    if($null -eq $Ast){throw 'Installer AST was not available for automatic-variable safety scan.'}
    $reserved=@('host','pid','psversiontable','pshome','psscriptroot','pscommandpath')
    $bad=@($Ast.FindAll({
        param($node)
        if($node -isnot [Management.Automation.Language.AssignmentStatementAst]){return $false}
        if($node.Left -isnot [Management.Automation.Language.VariableExpressionAst]){return $false}
        return $reserved -contains $node.Left.VariablePath.UserPath.ToLowerInvariant()
    },$true))
    if($bad.Count -gt 0){
        $detail=@($bad|ForEach-Object{('$'+$_.Left.VariablePath.UserPath+' at line '+$_.Extent.StartLineNumber)}) -join ', '
        throw ('Installer assigns a reserved PowerShell automatic variable: '+$detail)
    }
}

try{
    New-Item -ItemType Directory -Path $DataRoot -Force|Out-Null
    try{Start-Transcript -Path $TranscriptLog -Append|Out-Null}catch{}
    $PackagePath=[IO.Path]::GetFullPath($PackagePath)
    if(-not(Test-Path -LiteralPath $PackagePath -PathType Leaf)){throw ('Update package not found: '+$PackagePath)}
    if([IO.Path]::GetExtension($PackagePath) -ine '.yomiupdate'){throw 'Expected a .yomiupdate package.'}

    Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    New-Item -ItemType Directory -Path $Work -Force|Out-Null
    $zip=[IO.Compression.ZipFile]::OpenRead($PackagePath)
    try{
        $entries=@($zip.Entries|Where-Object{-not [string]::IsNullOrEmpty($_.Name)})
        $byName=@{}
        foreach($entry in $entries){
            $name=Assert-SafeRelativePath $entry.FullName
            if($byName.ContainsKey($name)){throw ('Duplicate package member: '+$name)}
            $byName[$name]=$entry
        }
        if(-not $byName.ContainsKey('package-manifest.json')){throw 'Package manifest is missing.'}
        $manifestBytes=Read-ZipEntryBytes $byName['package-manifest.json']
        $manifestText=[Text.Encoding]::UTF8.GetString($manifestBytes)
        $manifest=$manifestText|ConvertFrom-Json
        if([int]$manifest.schema -ne 1){throw 'Unsupported YOMI update manifest schema.'}
        if([string]::IsNullOrWhiteSpace([string]$manifest.product) -or -not([string]$manifest.product).StartsWith('YOMI 4.2.0.8 ',[StringComparison]::Ordinal)){throw 'Package product identity is not a supported YOMI 4.2.0.8 update.'}

        $incomingRevision=Get-OptionalManifestText $manifest 'revision'
        if([string]::IsNullOrWhiteSpace($incomingRevision)){$incomingRevision=Get-RevisionToken ([string]$manifest.product)}
        $requiredBaseRevision=Get-OptionalManifestText $manifest 'base_revision'
        $packageKind=Get-OptionalManifestText $manifest 'package_kind'
        $installedBuild=Join-Path $env:ProgramFiles 'YOMI\app\FOCUSED-BUILD.txt'
        $installedText=''
        if(Test-Path -LiteralPath $installedBuild -PathType Leaf){try{$installedText=[IO.File]::ReadAllText($installedBuild)}catch{}}
        $installedRevision=Get-RevisionToken $installedText
        if(-not [string]::IsNullOrWhiteSpace($incomingRevision) -and $installedText.IndexOf($incomingRevision,[StringComparison]::OrdinalIgnoreCase)-ge 0){
            $Success=$true
            return
        }
        if(-not [string]::IsNullOrWhiteSpace($installedRevision) -and [string]::IsNullOrWhiteSpace($requiredBaseRevision)){
            throw ('This installed focused build uses guarded updates. Package '+$incomingRevision+' does not declare base_revision, so it cannot safely replace '+$installedRevision+'. Use a current in-order .yomiupdate or the fallback setup executable.')
        }
        if(-not [string]::IsNullOrWhiteSpace($requiredBaseRevision)){
            if([string]::IsNullOrWhiteSpace($installedText)){throw ('Update package expects base '+$requiredBaseRevision+', but no installed focused-build identity could be read. Use the fallback setup executable.')}
            if($installedText.IndexOf($requiredBaseRevision,[StringComparison]::OrdinalIgnoreCase)-lt 0){throw ('Update package expects base '+$requiredBaseRevision+', but the installed focused build reports '+$installedRevision+'. Apply updates in order or use the fallback setup executable.')}
        }
        if(-not [string]::IsNullOrWhiteSpace($packageKind) -and $packageKind -ine 'focused-update'){throw ('Unsupported YOMI update package kind: '+$packageKind)}

        $expected=@('package-manifest.json')
        foreach($file in @($manifest.files)){$expected+=(Assert-SafeRelativePath ([string]$file.path))}
        $actual=@($byName.Keys|Sort-Object)
        $wanted=@($expected|Sort-Object)
        if(($actual -join "`n") -ne ($wanted -join "`n")){throw 'Package member set does not exactly match its manifest.'}

        # The elevated installer consumes package-manifest.json from the staging root.
        # Earlier hosts validated it in-memory but accidentally failed to stage it.
        Write-Bytes (Join-Path $Work 'package-manifest.json') $manifestBytes

        $workPrefix=([IO.Path]::GetFullPath($Work).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar)
        foreach($file in @($manifest.files)){
            $name=Assert-SafeRelativePath ([string]$file.path)
            if(-not $byName.ContainsKey($name)){throw ('Manifest member missing: '+$name)}
            $bytes=Read-ZipEntryBytes $byName[$name]
            if([long]$bytes.LongLength -ne [long]$file.bytes){throw ('Byte-length mismatch: '+$name)}
            $hash=Sha256-Bytes $bytes
            if($hash -ne ([string]$file.sha256).ToLowerInvariant()){throw ('SHA-256 mismatch: '+$name)}
            $dest=[IO.Path]::GetFullPath((Join-Path $Work ($name -replace '/','\')))
            if(-not $dest.StartsWith($workPrefix,[StringComparison]::OrdinalIgnoreCase)){throw ('Package path escaped staging root: '+$name)}
            Write-Bytes $dest $bytes
        }
    }finally{$zip.Dispose()}

    $installer=Join-Path $Work 'install.ps1'
    $tokens=$null;$errors=$null
    $installerAst=[Management.Automation.Language.Parser]::ParseFile($installer,[ref]$tokens,[ref]$errors)
    if($errors.Count -gt 0){throw ('Installer parse failed: '+(($errors|ForEach-Object{$_.Message}) -join ' | '))}
    Assert-NoReservedAutomaticVariableAssignment $installerAst

    $powershell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $psi=New-Object Diagnostics.ProcessStartInfo
    $psi.FileName=$powershell
    $psi.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$installer+'"'
    $psi.WorkingDirectory=$Work
    $psi.UseShellExecute=$false
    $psi.CreateNoWindow=$true
    $process=New-Object Diagnostics.Process
    $process.StartInfo=$psi
    if(-not $process.Start()){throw 'Could not start the staged YOMI installer.'}
    if(-not $process.WaitForExit(900000)){
        try{$process.Kill()}catch{}
        throw 'YOMI update exceeded the 15-minute safety ceiling.'
    }
    $code=[int]$process.ExitCode
    $process.Dispose()
    if($code -ne 0){throw ('YOMI installer exited with code '+$code+'. Check the newest quick-patch log under '+$DataRoot)}
    $Success=$true
}catch{
    $message='YOMI update package was not applied.'+"`r`n`r`n"+$_.Exception.Message+"`r`n`r`nLog: "+$Log
    try{Add-Content -LiteralPath $Log -Value ((Get-Date).ToString('O')+' ERROR '+$_.Exception.ToString()) -Encoding UTF8}catch{}
    Show-YomiMessage $message 'YOMI Update' $true
    exit 1
}finally{
    try{Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue}catch{}
    try{Stop-Transcript|Out-Null}catch{}
}
if($Success){exit 0}
exit 1
