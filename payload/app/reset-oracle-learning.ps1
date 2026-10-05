$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$stateRoot=Join-Path $DataRoot 'state'
$pending=Join-Path $stateRoot 'reset-oracle-learning.pending'
$capRoot=Join-Path $DataRoot 'cache\capabilities'

function Runtime-Alive {
    foreach($name in @('engine.pid','supervisor.pid')){
        $pidPath=Join-Path $stateRoot $name
        if(Test-Path $pidPath){
            $n=0
            try{[void][int]::TryParse((Get-Content $pidPath -Raw).Trim(),[ref]$n)}catch{}
            if($n -gt 0 -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $true}
        }
    }
    return $false
}

$answer=[System.Windows.Forms.MessageBox]::Show(
    "Reset YOMI's learned route compatibility and machine preparation timing?`r`n`r`nThis does NOT delete downloaded audio, artwork, video, visualizers, playlist state, settings, or history. YOMI will simply relearn compatibility and timing from future preparation work.",
    'YOMI Oracle Learning',
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Question
)
if($answer -ne [System.Windows.Forms.DialogResult]::Yes){Write-Host 'Oracle Learning reset cancelled.';exit 0}

if(Runtime-Alive){
    New-Item -ItemType Directory -Path $stateRoot -Force|Out-Null
    Set-Content $pending ([DateTime]::UtcNow.ToString('o')) -Encoding ASCII
    [System.Windows.Forms.MessageBox]::Show(
        "Oracle Learning will reset on the next YOMI startup.`r`n`r`nThe current playback session was left untouched.",
        'YOMI Oracle Learning',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    )|Out-Null
    Write-Host 'Oracle Learning reset queued for next startup.' -ForegroundColor Green
    exit 0
}

New-Item -ItemType Directory -Path $capRoot -Force|Out-Null
Get-ChildItem $capRoot -File -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction Stop
Remove-Item $pending -Force -ErrorAction SilentlyContinue
Write-Host 'Oracle Learning reset complete. Media cache was not touched.' -ForegroundColor Green
[System.Windows.Forms.MessageBox]::Show(
    'Oracle Learning reset complete. Media cache was not touched.',
    'YOMI Oracle Learning',
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information
)|Out-Null
