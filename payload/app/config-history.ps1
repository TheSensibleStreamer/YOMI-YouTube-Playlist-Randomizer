param(
    [ValidateSet('List','Restore','Snapshot')]
    [string]$Mode='List',
    [string]$Path='',
    [switch]$Force
)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

function Yomi-Running {
    foreach($name in @('supervisor.pid','engine.pid')){
        $pidPath=Join-Path $DataRoot ('state\'+$name)
        if(Test-Path $pidPath){
            $n=0
            try{[void][int]::TryParse((Get-Content $pidPath -Raw).Trim(),[ref]$n)}catch{}
            if($n -gt 0 -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $true}
        }
    }
    return $false
}
function History-Files {
    if(-not(Test-Path $script:ConfigHistoryRoot)){return @()}
    return @(Get-ChildItem $script:ConfigHistoryRoot -Filter 'config-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
}
function Describe([IO.FileInfo]$file){
    try{
        $cfg=Get-Content $file.FullName -Raw|ConvertFrom-Json
        [PSCustomObject]@{
            Time=$file.LastWriteTime
            Schema=$(if($null -ne $cfg.PSObject.Properties['config_schema_version']){[int]$cfg.config_schema_version}else{1})
            Mode=[string]$cfg.app_mode
            Performance=[string]$cfg.performance_mode
            File=$file.FullName
        }
    }catch{
        [PSCustomObject]@{Time=$file.LastWriteTime;Schema='?';Mode='UNREADABLE';Performance='';File=$file.FullName}
    }
}

switch($Mode){
    'List' {
        $files=History-Files
        if($files.Count -eq 0){Write-Host 'No Config Time Machine snapshots yet.';exit 0}
        $files|ForEach-Object{Describe $_}|Format-Table -AutoSize
    }
    'Snapshot' {
        if(Test-Path $script:ConfigPath){
            Save-YomiConfigHistorySnapshot -SourcePath $script:ConfigPath
            Write-Host 'Config Time Machine snapshot captured.' -ForegroundColor Green
        }else{throw 'config.json does not exist.'}
    }
    'Restore' {
        if(Yomi-Running){throw 'Stop YOMI playback before restoring an older configuration.'}
        if([string]::IsNullOrWhiteSpace($Path)){
            $picker=New-Object System.Windows.Forms.OpenFileDialog
            $picker.Title='YOMI Config Time Machine'
            $picker.Filter='YOMI config snapshot (config-*.json)|config-*.json|JSON files (*.json)|*.json'
            $picker.InitialDirectory=$script:ConfigHistoryRoot
            if($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){Write-Host 'Restore cancelled.';exit 0}
            $Path=$picker.FileName
        }
        if(-not(Test-Path $Path)){throw "Config snapshot not found: $Path"}
        $candidate=Get-Content $Path -Raw|ConvertFrom-Json
        $supported=Get-YomiContractSnapshot
        $schema=if($null -ne $candidate.PSObject.Properties['config_schema_version']){[int]$candidate.config_schema_version}else{1}
        Assert-YomiSchemaCompatible -Name 'Config Time Machine snapshot' -Actual $schema -Supported ([int]$supported.config_schema)

        if(-not $Force){
            $answer=[System.Windows.Forms.MessageBox]::Show(
                "Restore this YOMI configuration snapshot?`r`n`r`nThe current configuration will automatically become another Time Machine snapshot.",
                'YOMI Config Time Machine',
                [System.Windows.Forms.MessageBoxButtons]::YesNo,
                [System.Windows.Forms.MessageBoxIcon]::Question
            )
            if($answer -ne [System.Windows.Forms.DialogResult]::Yes){Write-Host 'Restore cancelled.';exit 0}
        }
        Save-YomiConfig $candidate
        Write-Host 'Configuration restored. Open Settings to review it before starting playback.' -ForegroundColor Green
    }
}
