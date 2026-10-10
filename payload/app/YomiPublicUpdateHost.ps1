param(
    [Parameter(Mandatory=$true)][string]$LatestVersion,
    [Parameter(Mandatory=$true)][string]$CurrentVersion,
    [Parameter(Mandatory=$true)][string]$UpdaterScript,
    [Parameter(Mandatory=$true)][string]$InstallRoot,
    [Parameter(Mandatory=$true)][string]$DataRoot
)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$statusFile=Join-Path (Split-Path $UpdaterScript -Parent) 'update-status.json'
Remove-Item -LiteralPath $statusFile -Force -ErrorAction SilentlyContinue

[xml]$xaml=@'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Name="UpdateWindow"
        Width="500" MinWidth="500" MaxWidth="500" MinHeight="152" MaxHeight="208" SizeToContent="Height"
        WindowStyle="None" ResizeMode="NoResize" AllowsTransparency="True"
        WindowStartupLocation="CenterScreen" Background="Transparent"
        ShowInTaskbar="True" Topmost="False" FontFamily="Segoe UI" FontSize="14"
        SnapsToDevicePixels="True" UseLayoutRounding="True">
  <Border BorderBrush="#383838" BorderThickness="2" Background="#171717">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="28"/>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Border x:Name="TitleBar" Grid.Row="0" Background="#202020" BorderBrush="#383838" BorderThickness="0,0,0,1">
        <Grid Margin="10,0,4,0">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="34"/>
          </Grid.ColumnDefinitions>
          <TextBlock x:Name="TitleText" VerticalAlignment="Center" Foreground="#F0F0EC" FontWeight="SemiBold" Text="YOMI Update"/>
          <Button x:Name="CloseButton" Grid.Column="1" Width="28" Height="22" Margin="0,2,0,2"
                  Background="Transparent" BorderThickness="0" Foreground="#C8C8C4"
                  IsEnabled="False">
            <Viewbox Width="9" Height="9" Stretch="Uniform">
              <Canvas Width="9" Height="9">
                <Path Stroke="#C8C8C4" StrokeThickness="1.35" StrokeStartLineCap="Square" StrokeEndLineCap="Square"
                      Data="M 1,1 L 8,8 M 8,1 L 1,8"/>
              </Canvas>
            </Viewbox>
          </Button>
        </Grid>
      </Border>
      <Grid Grid.Row="1" Margin="18,9,18,9">
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="12"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <TextBlock x:Name="VersionText" Foreground="#F0F0EC" FontSize="15" FontWeight="SemiBold"/>
        <TextBlock x:Name="StatusText" Grid.Row="1" Margin="0,7,0,0" Foreground="#BDBDB8" TextWrapping="Wrap"/>
        <Grid x:Name="ProgressTrack" Grid.Row="3" Height="9" Background="#292929" ClipToBounds="True" Visibility="Collapsed">
          <Border x:Name="ProgressFill" HorizontalAlignment="Left" Width="0" Background="#8D8D86"/>
        </Grid>
        <TextBlock x:Name="PercentText" Grid.Row="4" Margin="0,7,0,0" Foreground="#8F8F89" FontSize="12" Visibility="Collapsed"/>
        <Button x:Name="DoneButton" Grid.Row="5" HorizontalAlignment="Right" Margin="0,9,0,0" MinWidth="86" Height="29"
                Padding="12,0" Background="#252525" BorderBrush="#444440" BorderThickness="1"
                Foreground="#E8E8E3" Content="Close" Visibility="Collapsed"/>
      </Grid>
    </Grid>
  </Border>
</Window>
'@

$reader=New-Object System.Xml.XmlNodeReader $xaml
$window=[Windows.Markup.XamlReader]::Load($reader)
$titleBar=$window.FindName('TitleBar')
$titleText=$window.FindName('TitleText')
$closeButton=$window.FindName('CloseButton')
$versionText=$window.FindName('VersionText')
$statusText=$window.FindName('StatusText')
$progressTrack=$window.FindName('ProgressTrack')
$progressFill=$window.FindName('ProgressFill')
$percentText=$window.FindName('PercentText')
$doneButton=$window.FindName('DoneButton')

$titleText.Text='YOMI  '+$LatestVersion+'   Update available'
$versionText.Text='YOMI '+$LatestVersion+' is available. You have '+$CurrentVersion+'.'
$statusText.Text='A new version of YOMI is ready to install.'
$percentText.Text=''
$running=$false
$started=$false
$exitCode=$null
$updateProcess=$null
$lastState='ready'
$restartPending=$false
$restartLog=Join-Path $DataRoot 'update-restart.log'
$restartRelayPath=$null
$reportedProgress=0
$displayProgress=0.0
$closeButton.IsEnabled=$true
$doneButton.Content='Update'
$doneButton.Visibility='Visible'

function Set-Progress([int]$Percent,[string]$Message){
    $p=[Math]::Max(0,[Math]::Min(100,$Percent))
    $script:reportedProgress=$p
    if($script:displayProgress -lt $p){$script:displayProgress=[double]$p}
    $statusText.Text=$Message
    $percentText.Text=([Math]::Floor($script:displayProgress).ToString()+'%')
    $trackWidth=[Math]::Max(0,[double]$progressTrack.ActualWidth)
    $progressFill.Width=$trackWidth*($script:displayProgress/100.0)
}
function Write-RestartLog([string]$Message){
    try{
        $line=[DateTime]::Now.ToString('o')+' | '+$Message
        Add-Content -LiteralPath $restartLog -Value $line -Encoding UTF8
    }catch{}
}
function Test-YomiControllerRunning {
    try{
        $controller=[IO.Path]::GetFullPath((Join-Path $InstallRoot 'app\YomiControllerWpf.exe'))
        foreach($p in @(Get-CimInstance Win32_Process -Filter "Name='YomiControllerWpf.exe'" -ErrorAction SilentlyContinue)){
            try{
                if($p.ExecutablePath -and [IO.Path]::GetFullPath([string]$p.ExecutablePath) -eq $controller){return $true}
            }catch{}
        }
    }catch{}
    return $false
}
function Queue-DetachedRelaunch {
    try{
        # Always queue the detached post-verify check. A controller that is
        # momentarily running can crash while this updater host is closing.
        # The detached relay alone owns relaunch after install verification.
        $powershell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $relayName='YOMI-restart-'+([Guid]::NewGuid().ToString('N'))+'.ps1'
        $relayPath=Join-Path $env:TEMP $relayName
        $relayScript=@'
param(
    [Parameter(Mandatory=$true)][int]$ParentPid,
    [Parameter(Mandatory=$true)][string]$InstallRoot,
    [Parameter(Mandatory=$true)][string]$RestartLog
)
$ErrorActionPreference='SilentlyContinue'
function Log([string]$Message){
    try{Add-Content -LiteralPath $RestartLog -Value ([DateTime]::Now.ToString('o')+' | relay | '+$Message) -Encoding UTF8}catch{}
}
function Controller-Running {
    try{
        $target=[IO.Path]::GetFullPath((Join-Path $InstallRoot 'app\YomiControllerWpf.exe'))
        foreach($p in @(Get-CimInstance Win32_Process -Filter "Name='YomiControllerWpf.exe'" -ErrorAction SilentlyContinue)){
            try{if($p.ExecutablePath -and [IO.Path]::GetFullPath([string]$p.ExecutablePath) -eq $target){return $true}}catch{}
        }
    }catch{}
    return $false
}
function Shell-Open([string]$File,[string]$Arguments){
    try{
        $appDir=Join-Path $InstallRoot 'app'
        $shell=New-Object -ComObject Shell.Application
        $shell.ShellExecute($File,$Arguments,$appDir,'open',1)
        return $true
    }catch{Log ('shell launch exception: '+$_.Exception.Message);return $false}
}
Log ('relay started; waiting for updater host pid '+$ParentPid)
for($i=0;$i -lt 200;$i++){
    if(-not (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue)){break}
    Start-Sleep -Milliseconds 100
}
Start-Sleep -Milliseconds 500
if(Controller-Running){
    # A merely present process is not a successful restart. Give WPF rendering
    # and startup a short stability window, then recover if it died.
    Log 'controller found after updater exit; checking 3-second stability'
    Start-Sleep -Milliseconds 3000
    if(Controller-Running){
        Log 'controller remained running after startup stability check'
        try{Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force}catch{}
        exit 0
    }
    Log 'controller disappeared during startup stability check; trying recovery'
}
$appDir=Join-Path $InstallRoot 'app'
$controller=Join-Path $appDir 'YomiControllerWpf.exe'
$launcher=Join-Path $appDir 'YomiLauncher.exe'
if(Test-Path -LiteralPath $controller -PathType Leaf){
    Log 'shell-broker direct controller launch'
    [void](Shell-Open $controller '')
    Start-Sleep -Milliseconds 3000
}
if(-not (Controller-Running) -and (Test-Path -LiteralPath $launcher -PathType Leaf)){
    Log 'direct launch did not stay up; shell-broker launcher fallback'
    [void](Shell-Open $launcher 'controller')
    Start-Sleep -Milliseconds 3500
}
if(-not (Controller-Running) -and (Test-Path -LiteralPath $controller -PathType Leaf)){
    Log 'launcher fallback did not stay up; final direct retry'
    [void](Shell-Open $controller '')
    Start-Sleep -Milliseconds 3500
}
if(Controller-Running){Log 'restart verified after updater host exit'}else{Log 'restart failed after detached relay attempts'}
try{Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force}catch{}
'@
        Set-Content -LiteralPath $relayPath -Value $relayScript -Encoding UTF8
        $script:restartRelayPath=$relayPath
        $args='-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$relayPath+'" -ParentPid '+$PID+' -InstallRoot "'+$InstallRoot+'" -RestartLog "'+$restartLog+'"'
        $shell=New-Object -ComObject Shell.Application
        $shell.ShellExecute($powershell,$args,$env:TEMP,'open',0)
        Write-RestartLog ('detached shell relay queued: '+$relayPath)
        return $true
    }catch{
        Write-RestartLog ('could not queue detached shell relay: '+$_.Exception.Message)
        try{if($relayPath -and (Test-Path -LiteralPath $relayPath)){Remove-Item -LiteralPath $relayPath -Force}}catch{}
        return $false
    }
}
function Relaunch-Yomi {
    try{
        if(Test-YomiControllerRunning){Write-RestartLog 'controller already running';return $true}
        $appDir=Join-Path $InstallRoot 'app'
        $controller=Join-Path $appDir 'YomiControllerWpf.exe'
        if(Test-Path -LiteralPath $controller -PathType Leaf){
            $psi=New-Object Diagnostics.ProcessStartInfo
            $psi.FileName=$controller
            $psi.WorkingDirectory=$appDir
            $psi.UseShellExecute=$false
            $psi.CreateNoWindow=$false
            $p=[Diagnostics.Process]::Start($psi)
            if($p){Write-RestartLog ('direct controller launch pid '+$p.Id)}
            Start-Sleep -Milliseconds 1400
            if(Test-YomiControllerRunning){Write-RestartLog 'direct controller launch verified';return $true}
            try{if($p -and $p.HasExited){Write-RestartLog ('direct controller exited '+$p.ExitCode)}}catch{}
        }else{Write-RestartLog 'controller executable missing'}
        $launcher=Join-Path $appDir 'YomiLauncher.exe'
        if(Test-Path -LiteralPath $launcher -PathType Leaf){
            $lp=Start-Process -FilePath $launcher -ArgumentList 'controller' -WorkingDirectory $appDir -PassThru
            if($lp){Write-RestartLog ('launcher fallback pid '+$lp.Id)}
            Start-Sleep -Milliseconds 1600
            if(Test-YomiControllerRunning){Write-RestartLog 'launcher fallback verified';return $true}
        }else{Write-RestartLog 'launcher executable missing'}
    }catch{Write-RestartLog ('restart exception: '+$_.Exception.Message)}
    Write-RestartLog 'restart not verified'
    return $false
}
function Finish-Host([int]$Code){
    if(-not $script:running){return}
    $script:running=$false
    $script:exitCode=$Code
    $closeButton.IsEnabled=$true
    $doneButton.Content='Close'
    $doneButton.Visibility='Visible'
    if($Code -eq 0){
        $installed=$LatestVersion
        try{
            $versionPath=Join-Path $InstallRoot 'VERSION.txt'
            if(Test-Path -LiteralPath $versionPath -PathType Leaf){
                $raw=(Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8).Trim()
                if(-not [string]::IsNullOrWhiteSpace($raw)){$installed=$raw}
            }
        }catch{}
        $titleText.Text='YOMI  '+$installed+'   Installed'
        $versionText.Text='YOMI '+$installed+' is installed.'
        Set-Progress 100 'Update installed and verified. Restarting YOMI...'
        $percentText.Text='Installed and verified'
        $doneButton.Visibility='Collapsed'
        $closeButton.IsEnabled=$false
        if(Queue-DetachedRelaunch){
            $script:restartPending=$false
            Write-RestartLog 'detached restart relay owns the single post-update relaunch'
            $window.Close()
        }else{
            $script:restartPending=$true
            Set-Progress 100 ('YOMI '+$installed+' is installed, but the restart handoff could not be queued.')
            $percentText.Text='Installed'
            $doneButton.Content='Open YOMI'
            $doneButton.Visibility='Visible'
            $closeButton.IsEnabled=$true
        }
    }
    elseif($Code -eq 4){
        Set-Progress 100 'The update failed verification. The last working YOMI installation was restored.'
        $percentText.Text='Rolled back safely'
        Relaunch-Yomi
    }
    else {
        if($lastState -ne 'error'){Set-Progress 100 'YOMI could not complete the update. The existing installation was left protected.'}
        $percentText.Text='Update failed'
    }
}

$titleBar.Add_MouseLeftButtonDown({
    try{if($_.ButtonState -eq [Windows.Input.MouseButtonState]::Pressed){$window.DragMove()}}catch{}
})
$closeButton.Add_Click({if(-not $running){$window.Close()}})
$window.Add_Closing({if($running){$_.Cancel=$true}})

$timer=New-Object Windows.Threading.DispatcherTimer
$timer.Interval=[TimeSpan]::FromMilliseconds(160)
$timer.Add_Tick({
    try{
        if(Test-Path -LiteralPath $statusFile -PathType Leaf){
            $s=Get-Content -LiteralPath $statusFile -Raw -Encoding UTF8|ConvertFrom-Json
            $script:lastState=[string]$s.state
            Set-Progress ([int]$s.percent) ([string]$s.message)
        }
    }catch{}

    # Long compile/extract steps can be healthy while the installer has no new discrete
    # percentage to report. During those installation phases, let the bar creep within a
    # small bounded headroom so it visibly remains alive without ever reaching completion early.
    if($script:running -and $script:lastState -eq 'installing' -and
       $script:reportedProgress -ge 60 -and $script:reportedProgress -lt 99){
        $cap=[Math]::Min(98.5,[double]$script:reportedProgress+2.5)
        if($script:displayProgress -lt $cap){
            $script:displayProgress=[Math]::Min($cap,$script:displayProgress+0.10)
            $trackWidth=[Math]::Max(0,[double]$progressTrack.ActualWidth)
            $progressFill.Width=$trackWidth*($script:displayProgress/100.0)
            $percentText.Text=([Math]::Floor($script:displayProgress).ToString()+'%')
        }
    }

    if($null -ne $updateProcess -and $updateProcess.HasExited){
        $timer.Stop()
        Finish-Host ([int]$updateProcess.ExitCode)
        try{$updateProcess.Dispose()}catch{}
    }
})

function Start-YomiPublicUpdate {
    if($script:started){return}
    $script:started=$true
    $script:running=$true
    $closeButton.IsEnabled=$false
    $doneButton.Visibility='Collapsed'
    $progressTrack.Visibility='Visible'
    $percentText.Visibility='Visible'
    $titleText.Text='YOMI  '+$LatestVersion+'   Updating'
    try{
        $powershell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $args='-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$UpdaterScript+'" -Manual -Approved -StatusFile "'+$statusFile+'" -InstallRootOverride "'+$InstallRoot+'" -DataRootOverride "'+$DataRoot+'"'
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=$powershell
        $psi.Arguments=$args
        $psi.WorkingDirectory=Split-Path $UpdaterScript -Parent
        $psi.UseShellExecute=$false
        $psi.CreateNoWindow=$true
        $psi.WindowStyle=[Diagnostics.ProcessWindowStyle]::Hidden
        $script:updateProcess=New-Object Diagnostics.Process
        $script:updateProcess.StartInfo=$psi
        if(-not $script:updateProcess.Start()){throw 'Could not start the YOMI update engine.'}
        $timer.Start()
        Set-Progress 1 'Starting the YOMI update engine...'
    }catch{
        $script:lastState='error'
        $script:running=$false
        $closeButton.IsEnabled=$true
        $doneButton.Content='Close'
        $doneButton.Visibility='Visible'
        Set-Progress 100 ('Could not start the updater: '+$_.Exception.Message)
        $percentText.Text='Update failed'
    }
}
$doneButton.Add_Click({
    if(-not $started){Start-YomiPublicUpdate}
    elseif(-not $running){
        if($restartPending){
            if(Relaunch-Yomi){$script:restartPending=$false;$window.Close()}
            else{Set-Progress 100 'YOMI is installed, but Windows still did not reopen it.';$percentText.Text='Installed'}
        }else{$window.Close()}
    }
})

[void]$window.ShowDialog()
