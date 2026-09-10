param([switch]$Elevated)
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Windows.Forms
$installRoot=Join-Path $env:ProgramFiles 'YOMI';$dataRoot=Join-Path $env:LOCALAPPDATA 'YOMI';$defenderMarker=Join-Path $dataRoot 'defender-yt-dlp-process-exclusion.txt';$defenderTarget=Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'
if(-not $Elevated){$temp=Join-Path $env:TEMP ('YOMI-Uninstall-'+[Guid]::NewGuid().ToString('N')+'.ps1');Copy-Item $PSCommandPath $temp -Force;$e=$temp.Replace("'","''");$p=Start-Process powershell.exe -Verb RunAs -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$e+"' -Elevated")) -Wait -PassThru;exit $p.ExitCode}
$answer=[System.Windows.Forms.MessageBox]::Show("Remove YOMI user data too?`r`n`r`nYES = Remove everything.`r`nNO = Keep only config.json and playlist.txt for a future reinstall; disposable caches, logs and installer downloads are removed.`r`nCANCEL = Do nothing.",'Uninstall YOMI',[System.Windows.Forms.MessageBoxButtons]::YesNoCancel,[System.Windows.Forms.MessageBoxIcon]::Question);if($answer -eq [System.Windows.Forms.DialogResult]::Cancel){exit 0}
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

# Remove only the exact process exclusion that YOMI recorded as its own.
# A matching exclusion without the marker belongs to the user and is untouched.
if(Test-Path $defenderMarker){
    try{
        $marked=(Get-Content $defenderMarker -Raw -ErrorAction Stop).Trim()
        if(-not [string]::Equals($marked,$defenderTarget,[StringComparison]::OrdinalIgnoreCase)){throw 'The Defender ownership marker does not contain the expected YOMI yt-dlp path.'}
        $present=$false
        foreach($item in @((Get-MpPreference -ErrorAction Stop).ExclusionProcess)){
            $expanded=[Environment]::ExpandEnvironmentVariables([string]$item)
            if([string]::Equals($expanded,$defenderTarget,[StringComparison]::OrdinalIgnoreCase)){$present=$true;break}
        }
        if($present){
            Remove-MpPreference -ExclusionProcess $defenderTarget -ErrorAction Stop
            foreach($item in @((Get-MpPreference -ErrorAction Stop).ExclusionProcess)){
                $expanded=[Environment]::ExpandEnvironmentVariables([string]$item)
                if([string]::Equals($expanded,$defenderTarget,[StringComparison]::OrdinalIgnoreCase)){throw 'Windows Defender did not remove the YOMI-managed yt-dlp exclusion.'}
            }
        }
        Remove-Item $defenderMarker -Force -ErrorAction Stop
    }catch{
        [System.Windows.Forms.MessageBox]::Show("YOMI stopped uninstalling because its Windows Defender exclusion could not be safely removed.`r`n`r`n$($_.Exception.Message)`r`n`r`nNothing else was deleted. Run the uninstaller again after Windows Defender is available.",'Uninstall YOMI',[System.Windows.Forms.MessageBoxButtons]::OK,[System.Windows.Forms.MessageBoxIcon]::Error)|Out-Null
        exit 11
    }
}
$start=Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\YOMI';Remove-Item $start -Recurse -Force -ErrorAction SilentlyContinue;$desktop=[Environment]::GetFolderPath('Desktop');Get-ChildItem $desktop -Filter 'YOMI*.lnk' -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue
if($answer -eq [System.Windows.Forms.DialogResult]::Yes){Remove-Item $dataRoot -Recurse -Force -ErrorAction SilentlyContinue}else{$tmpKeep=Join-Path $env:TEMP ('YOMI-Keep-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $tmpKeep -Force|Out-Null;foreach($n in @('config.json','playlist.txt')){$p=Join-Path $dataRoot $n;if(Test-Path $p){Copy-Item $p (Join-Path $tmpKeep $n) -Force}};Remove-Item $dataRoot -Recurse -Force -ErrorAction SilentlyContinue;New-Item -ItemType Directory -Path $dataRoot -Force|Out-Null;Get-ChildItem $tmpKeep -File -ErrorAction SilentlyContinue|Copy-Item -Destination $dataRoot -Force;Remove-Item $tmpKeep -Recurse -Force -ErrorAction SilentlyContinue}
[System.Windows.Forms.MessageBox]::Show('YOMI was removed. Unrelated media-player installations were not modified.','Uninstall YOMI')|Out-Null
