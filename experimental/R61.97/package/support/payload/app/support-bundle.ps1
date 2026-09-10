param([switch]$NoOpen)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$supportRoot=Join-Path $DataRoot 'support'
New-Item -ItemType Directory -Path $supportRoot -Force|Out-Null
$work=Join-Path ([IO.Path]::GetTempPath()) ('yomi-support-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work|Out-Null
$zip=Join-Path $supportRoot ("YOMI-support-$stamp.zip")

function Write-Text([string]$name,[string]$text){
    [IO.File]::WriteAllText((Join-Path $work $name),$text,(New-Object System.Text.UTF8Encoding($false)))
}
function Sanitize([string]$text){
    if($null -eq $text){return ''}
    $t=$text -replace 'https?://[^\s"''<>]+','<url-redacted>'
    $t=$t -replace '(?i)(playlist=)[^\s&"''<>]+','$1<redacted>'
    return $t
}
function Tail-Sanitized([string]$path,[int]$lines=200){
    if(-not(Test-Path $path)){return ''}
    return Sanitize ((Get-Content $path -Tail $lines -ErrorAction SilentlyContinue) -join "`r`n")
}

try{
    Write-Text 'README.txt' @"
YOMI support bundle
Generated: $(Get-Date -Format o)

Privacy policy:
- Playlist URL is redacted.
- playlist.txt is NOT included.
- Full session URLs are NOT included.
- Comments and media cache files are NOT included.
- Logs are tail-limited and URL-redacted.
- This ZIP stays local until you explicitly share it.
"@

    $doctor=Join-Path $PSScriptRoot 'doctor.ps1'
    if(Test-Path $doctor){
        $doctorOut=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $doctor 2>&1|Out-String
        Write-Text 'doctor.txt' (Sanitize $doctorOut)
    }

    $self=Join-Path $PSScriptRoot 'self-test.ps1'
    if(Test-Path $self){
        $selfOut=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $self -Quick 2>&1|Out-String
        Write-Text 'self-test.txt' (Sanitize $selfOut)
    }

    $cfg=Get-YomiConfig
    $cfg.playlist='<redacted>'
    Write-Text 'config.redacted.json' ($cfg|ConvertTo-Json -Depth 12)
    try{Write-Text 'contracts.json' ((Get-YomiContractSnapshot)|ConvertTo-Json -Depth 5)}catch{}

    $stateRoot=Join-Path $DataRoot 'state'
    $sessionPath=Join-Path $stateRoot 'session.json'
    if(Test-Path $sessionPath){
        try{
            $session=Get-Content $sessionPath -Raw|ConvertFrom-Json
            $sessionSummary=[ordered]@{
                schema=$session.schema;session_id=$session.session_id;created_utc=$session.created_utc;
                count=$session.count;order_sha256=$session.order_sha256;metadata_snapshot=$session.metadata_snapshot
            }
            Write-Text 'session-summary.json' ($sessionSummary|ConvertTo-Json -Depth 6)
        }catch{}
    }

    foreach($name in @('runtime-instance.json','runtime-lease.json','watchdog-status.json','preflight.json','engine-status.json','rehearsal-report.json','freeze-window.json','update-transaction.json','session-journal-head.json','aegis-repair.json')){
        $statePath=Join-Path $stateRoot $name
        if(Test-Path $statePath){Write-Text $name (Sanitize (Get-Content $statePath -Raw))}
    }

    $queuePath=Join-Path $stateRoot 'queue-runtime.json'
    if(Test-Path $queuePath){
        try{
            $q=Get-Content $queuePath -Raw|ConvertFrom-Json
            $compact=[ordered]@{
                schema=$q.schema;revision=$q.revision;runtime_id=$q.runtime_id;session_id=$q.session_id;
                current_index=$q.current_index;current_order_slot=$q.current_order_slot;count=$q.count;
                base_occurrences=$q.base_occurrences;registry_occurrences=$q.registry_occurrences;inserted_occurrences=$q.inserted_occurrences;
                order_revision=$q.order_revision;active_jobs=$q.active_jobs;queued_jobs=$q.queued_jobs;
                workers=$q.workers;buffer_health=$q.buffer_health;ready_ahead=$q.ready_ahead;
                ready_time_seconds=$q.ready_time_seconds;failure_domain=$q.failure_domain;disk=$q.disk;
                scheduler=$q.scheduler;oracle=$q.oracle;service_level=$q.service_level;rehearsal=$q.rehearsal;freeze=$q.freeze;object_store=$q.object_store
            }
            Write-Text 'queue-runtime-summary.json' ($compact|ConvertTo-Json -Depth 8)
        }catch{}
    }

    $orderPath=Join-Path $stateRoot 'session-order.json'
    if(Test-Path $orderPath){
        try{
            $o=Get-Content $orderPath -Raw|ConvertFrom-Json
            $summary=[ordered]@{
                schema=$o.schema;session_id=$o.session_id;base_count=$o.base_count;
                next_occurrence_id=$o.next_occurrence_id;revision=$o.revision;active_count=$o.active_count;
                order_count=@($o.order).Count;inserted_count=@($o.inserted).Count;
                undo_count=@($o.undo).Count;redo_count=@($o.redo).Count;updated_unix=$o.updated_unix
            }
            Write-Text 'session-order-summary.json' ($summary|ConvertTo-Json -Depth 6)
        }catch{}
    }

    foreach($notice in @('config-recovery.txt','config-incompatible.txt')){
        $src=Join-Path $stateRoot $notice
        if(Test-Path $src){Write-Text $notice (Sanitize (Get-Content $src -Raw))}
    }
    $installStatus=Join-Path $DataRoot 'install-status.txt'
    if(Test-Path $installStatus){Write-Text 'install-status.txt' (Sanitize (Get-Content $installStatus -Raw))}

    foreach($pair in @(@('session-journal.jsonl',100),@('events.jsonl',250))){
        $src=Join-Path $stateRoot $pair[0]
        if(Test-Path $src){Write-Text ($pair[0]+'-tail.txt') (Tail-Sanitized $src ([int]$pair[1]))}
    }

    $performanceMemoryFile=Join-Path $DataRoot 'cache\capabilities\performance-memory.json'
    if(Test-Path $performanceMemoryFile){
        try{
            $perfMemory=Get-Content $performanceMemoryFile -Raw|ConvertFrom-Json
            $perfRows=@($perfMemory.entries.PSObject.Properties|ForEach-Object{
                [ordered]@{profile=$_.Name;samples=$_.Value.samples;ewma_seconds=$_.Value.ewma_seconds;ewma_deviation=$_.Value.ewma_deviation;updated_unix=$_.Value.updated_unix}
            })
            Write-Text 'performance-memory-summary.json' (([ordered]@{schema=$perfMemory.schema;profiles=$perfRows})|ConvertTo-Json -Depth 7)
        }catch{}
    }

    $capabilityFile=Join-Path $DataRoot 'cache\capabilities\video-route-memory.json'
    if(Test-Path $capabilityFile){
        try{
            $cap=Get-Content $capabilityFile -Raw|ConvertFrom-Json
            $capSummary=[ordered]@{schema=$cap.schema;entries=@($cap.entries.PSObject.Properties).Count;bytes=[int64](Get-Item $capabilityFile).Length}
            Write-Text 'route-intelligence-summary.json' ($capSummary|ConvertTo-Json -Depth 4)
        }catch{}
    }
    $oracleReports=@(Get-ChildItem (Join-Path $DataRoot 'oracle-lab') -Filter 'oracle-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
    if($oracleReports.Count -gt 0){try{Copy-Item $oracleReports[0].FullName (Join-Path $work 'oracle-lab-latest.json') -Force}catch{}}

    $configHistory=@(Get-ChildItem (Join-Path $DataRoot 'config-history') -Filter 'config-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
    $configHistorySummary=@($configHistory|ForEach-Object{
        [ordered]@{utc=$_.LastWriteTimeUtc.ToString('o');bytes=[int64]$_.Length;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
    })
    Write-Text 'config-history-summary.json' ($configHistorySummary|ConvertTo-Json -Depth 4)

    $incidentTool=Join-Path $PSScriptRoot 'incident-replay.ps1'
    if(Test-Path $incidentTool){
        try{
            $incidentOut=Join-Path $work 'incident-replay.json'
            $incidentConsole=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $incidentTool -OutputPath $incidentOut 2>&1|Out-String
            Write-Text 'incident-replay-console.txt' (Sanitize $incidentConsole)
        }catch{}
    }

    $component=@()
    foreach($name in @('mpv','ytdlp','deno','ffmpeg')){$component+=Get-YomiComponentInfo $name}
    Write-Text 'components.json' ($component|ConvertTo-Json -Depth 5)

    $logs=Join-Path $DataRoot 'logs'
    foreach($name in @('mpv.log','supervisor.log','server-error.log')){
        $src=Join-Path $logs $name
        if(Test-Path $src){Write-Text ("tail-"+$name) (Tail-Sanitized $src 250)}
    }

    $bundleFiles=@()
    foreach($file in @(Get-ChildItem $work -File|Sort-Object Name)){
        $bundleFiles+=[ordered]@{
            name=$file.Name
            bytes=[int64]$file.Length
            sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $bundleManifest=[ordered]@{
        schema=1
        product='YOMI'
        bundle_type='privacy-conscious-support'
        generated_utc=[DateTime]::UtcNow.ToString('o')
        files=$bundleFiles
    }
    Write-Text 'bundle-manifest.json' ($bundleManifest|ConvertTo-Json -Depth 6)

    if(Test-Path $zip){Remove-Item $zip -Force}
    Compress-Archive -Path (Join-Path $work '*') -DestinationPath $zip -CompressionLevel Optimal
    Set-Clipboard $zip
    Write-Host "Support bundle created: $zip"
    Write-Host 'Path copied to clipboard.'
    if(-not $NoOpen){Start-Process explorer.exe -ArgumentList @('/select,',('"'+$zip+'"'))}
}finally{
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
