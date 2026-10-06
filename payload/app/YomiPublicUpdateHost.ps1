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
        Width="500" Height="232" MinWidth="500" MinHeight="232" MaxWidth="500" MaxHeight="232"
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
      <Grid Grid.Row="1" Margin="18,16,18,15">
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="18"/>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="*"/>
          <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <TextBlock x:Name="VersionText" Foreground="#F0F0EC" FontSize="15" FontWeight="SemiBold"/>
        <TextBlock x:Name="StatusText" Grid.Row="1" Margin="0,7,0,0" Foreground="#BDBDB8" TextWrapping="Wrap"/>
        <Grid x:Name="ProgressTrack" Grid.Row="3" Height="9" Background="#292929" ClipToBounds="True" Visibility="Collapsed">
          <Border x:Name="ProgressFill" HorizontalAlignment="Left" Width="0" Background="#8D8D86"/>
        </Grid>
        <TextBlock x:Name="PercentText" Grid.Row="4" Margin="0,7,0,0" Foreground="#8F8F89" FontSize="12" Visibility="Collapsed"/>
        <Button x:Name="DoneButton" Grid.Row="5" HorizontalAlignment="Right" MinWidth="86" Height="29"
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
$closeButton.IsEnabled=$true
$doneButton.Content='Update'
$doneButton.Visibility='Visible'

function Set-Progress([int]$Percent,[string]$Message){
    $p=[Math]::Max(0,[Math]::Min(100,$Percent))
    $statusText.Text=$Message
    $percentText.Text=($p.ToString()+'%')
    $trackWidth=[Math]::Max(0,[double]$progressTrack.ActualWidth)
    $progressFill.Width=$trackWidth*($p/100.0)
}
function Relaunch-Yomi {
    try{
        $launcher=Join-Path $InstallRoot 'app\YomiLauncher.exe'
        if(Test-Path -LiteralPath $launcher -PathType Leaf){
            Start-Process -FilePath $launcher -ArgumentList 'controller' -WorkingDirectory (Split-Path $launcher -Parent)
        }
    }catch{}
}
function Finish-Host([int]$Code){
    if(-not $script:running){return}
    $script:running=$false
    $script:exitCode=$Code
    $closeButton.IsEnabled=$true
    $doneButton.Content='Close'
    $doneButton.Visibility='Visible'
    if($Code -eq 0){
        Set-Progress 100 'Update complete. Restarting YOMI...'
        $percentText.Text='Installed and verified'
        $doneButton.Visibility='Collapsed'
        $closeButton.IsEnabled=$false
        $restartTimer=New-Object Windows.Threading.DispatcherTimer
        $restartTimer.Interval=[TimeSpan]::FromMilliseconds(800)
        $restartTimer.Add_Tick({
            $restartTimer.Stop()
            Relaunch-Yomi
            $window.Close()
        })
        $restartTimer.Start()
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
    elseif(-not $running){$window.Close()}
})

[void]$window.ShowDialog()
