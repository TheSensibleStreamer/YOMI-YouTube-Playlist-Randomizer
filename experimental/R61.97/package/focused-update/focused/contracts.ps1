$script:YomiContractVersions = [ordered]@{
    config_schema = 2
    session_schema = 2
    occurrence_order_schema = 3
    session_journal_schema = 2
    aegis_repair_schema = 1
    resource_governor_schema = 1
    queue_runtime_schema = 4
    runtime_instance_schema = 4
    runtime_lease_schema = 1
    update_transaction_schema = 1
    watchdog_status_schema = 1
    recovery_point_schema = 1
    incident_replay_schema = 1
    metrics_schema = 1
    preflight_schema = 1
    route_intelligence_schema = 2
    performance_memory_schema = 1
    oracle_lab_schema = 1
    browser_protocol = 1
    cache_identity_schema = 1
    support_bundle_schema = 1
    build_manifest_schema = 1
}

function Get-YomiContractSnapshot {
    [PSCustomObject][ordered]@{
        config_schema = [int]$script:YomiContractVersions.config_schema
        session_schema = [int]$script:YomiContractVersions.session_schema
        occurrence_order_schema = [int]$script:YomiContractVersions.occurrence_order_schema
        session_journal_schema = [int]$script:YomiContractVersions.session_journal_schema
        aegis_repair_schema = [int]$script:YomiContractVersions.aegis_repair_schema
        resource_governor_schema = [int]$script:YomiContractVersions.resource_governor_schema
        queue_runtime_schema = [int]$script:YomiContractVersions.queue_runtime_schema
        runtime_instance_schema = [int]$script:YomiContractVersions.runtime_instance_schema
        runtime_lease_schema = [int]$script:YomiContractVersions.runtime_lease_schema
        update_transaction_schema = [int]$script:YomiContractVersions.update_transaction_schema
        watchdog_status_schema = [int]$script:YomiContractVersions.watchdog_status_schema
        recovery_point_schema = [int]$script:YomiContractVersions.recovery_point_schema
        incident_replay_schema = [int]$script:YomiContractVersions.incident_replay_schema
        metrics_schema = [int]$script:YomiContractVersions.metrics_schema
        preflight_schema = [int]$script:YomiContractVersions.preflight_schema
        route_intelligence_schema = [int]$script:YomiContractVersions.route_intelligence_schema
        performance_memory_schema = [int]$script:YomiContractVersions.performance_memory_schema
        oracle_lab_schema = [int]$script:YomiContractVersions.oracle_lab_schema
        browser_protocol = [int]$script:YomiContractVersions.browser_protocol
        cache_identity_schema = [int]$script:YomiContractVersions.cache_identity_schema
        support_bundle_schema = [int]$script:YomiContractVersions.support_bundle_schema
        build_manifest_schema = [int]$script:YomiContractVersions.build_manifest_schema
    }
}

function Assert-YomiSchemaCompatible {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][int]$Actual,
        [Parameter(Mandatory=$true)][int]$Supported
    )
    if($Actual -gt $Supported){
        throw "$Name schema $Actual is newer than this YOMI build supports ($Supported)."
    }
    if($Actual -lt 0){throw "$Name schema cannot be negative."}
}
