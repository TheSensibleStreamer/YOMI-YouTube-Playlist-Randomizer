param([switch]$Elevated)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Windows.Forms
$installRoot=Join-Path $env:ProgramFiles 'YOMI';$dataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'
if(-not $Elevated){$temp=Join-Path $env:TEMP ('YOMI-Uninstall-'+[Guid]::NewGuid().ToString('N')+'.ps1');Copy-Item $PSCommandPath $temp -Force;$e=$temp.Replace("'","''");$p=Start-Process powershell.exe -Verb RunAs -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$e+"' -Elevated")) -Wait -PassThru;exit $p.ExitCode}
$answer=[System.Windows.Forms.MessageBox]::Show("Remove YOMI user data too?`r`n`r`nYES = Remove everything.`r`nNO = Keep only config.json and playlist.txt for a future reinstall; disposable caches, logs and installer downloads are removed.`r`nCANCEL = Do nothing.",'Uninstall YOMI',[System.Windows.Forms.MessageBoxButtons]::YesNoCancel,[System.Windows.Forms.MessageBoxIcon]::Question);if($answer -eq [System.Windows.Forms.DialogResult]::Cancel){exit 0}
$defenderNote=''
$defenderMarker=Join-Path $dataRoot 'defender-performance-opt-in.txt'
$expectedDefenderProcess=Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'
if(Test-Path $defenderMarker){
    try{
        $markedPath=(Get-Content $defenderMarker -Raw -ErrorAction Stop).Trim()
        if($markedPath -ieq $expectedDefenderProcess){
            Remove-MpPreference -ExclusionProcess $expectedDefenderProcess -ErrorAction Stop
            $defenderNote=([Environment]::NewLine+[Environment]::NewLine+'The YOMI-managed Windows Defender exclusion was removed.')
        }
    }catch{
        $defenderNote=([Environment]::NewLine+[Environment]::NewLine+"WARNING: Windows Defender would not remove YOMI's process exclusion. You can remove it manually from Windows Security.")
    }
}
foreach($pidName in @('engine.pid','server.pid','supervisor.pid')){$f=Join-Path $dataRoot ('state\'+$pidName);if(Test-Path $f){$n=0;try{[void][int]::TryParse((Get-Content $f -Raw).Trim(),[ref]$n)}catch{};if($n -gt 0 -and $n -ne $PID){Stop-Process -Id $n -Force -ErrorAction SilentlyContinue}}}

# Elevated uninstall runs from a temp copy, so the installed UI processes can
# be closed before Program Files is removed.
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.ProcessId -ne $PID -and $_.CommandLine -and (
            $_.CommandLine -like '*C:\Program Files\YOMI\app\controller.ps1*' -or
            $_.CommandLine -like '*C:\Program Files\YOMI\app\settings.ps1*' -or
            $_.CommandLine -like '*C:\Program Files\YOMI\app\server.ps1*' -or
            $_.CommandLine -like '*C:\Program Files\YOMI\app\supervisor.ps1*'
        )
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }

Start-Sleep -Milliseconds 350
$start=Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\YOMI';Remove-Item $start -Recurse -Force -ErrorAction SilentlyContinue;$desktop=[Environment]::GetFolderPath('Desktop');Get-ChildItem $desktop -Filter 'YOMI*.lnk' -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue
if($answer -eq [System.Windows.Forms.DialogResult]::Yes){Remove-Item $dataRoot -Recurse -Force -ErrorAction SilentlyContinue}else{$tmpKeep=Join-Path $env:TEMP ('YOMI-Keep-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $tmpKeep -Force|Out-Null;foreach($n in @('config.json','playlist.txt')){$p=Join-Path $dataRoot $n;if(Test-Path $p){Copy-Item $p (Join-Path $tmpKeep $n) -Force}};Remove-Item $dataRoot -Recurse -Force -ErrorAction SilentlyContinue;New-Item -ItemType Directory -Path $dataRoot -Force|Out-Null;Get-ChildItem $tmpKeep -File -ErrorAction SilentlyContinue|Copy-Item -Destination $dataRoot -Force;Remove-Item $tmpKeep -Recurse -Force -ErrorAction SilentlyContinue}
[System.Windows.Forms.MessageBox]::Show(('YOMI was removed. Unrelated media-player installations were not modified.'+$defenderNote),'Uninstall YOMI')|Out-Null
