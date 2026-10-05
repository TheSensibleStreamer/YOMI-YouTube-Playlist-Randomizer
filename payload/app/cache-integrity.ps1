param(
    [switch]$DeepHash,
    [switch]$RepairReceipts,
    [switch]$SealHashes,
    [switch]$CopyReport,
    [switch]$OpenReport
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$objectRoot=Join-Path $DataRoot 'cache\objects'
$receiptRoot=Join-Path $objectRoot 'receipts'
$quarantineRoot=Join-Path $objectRoot 'quarantine'
$reportRoot=Join-Path $DataRoot 'reports'
foreach($p in @($receiptRoot,$quarantineRoot,$reportRoot)){New-Item -ItemType Directory -Path $p -Force|Out-Null}

function Get-IdentityHash([byte[]]$Bytes){
    function H([byte[]]$b,[uint64]$seed){
        [uint64]$h=$seed
        foreach($x in $b){$h=(($h*65599)+[uint64]$x+17) % 4294967296}
        return ('{0:x8}' -f [uint32]$h)
    }
    return (H $Bytes 2166136261)+(H $Bytes 2246822519)
}
function Get-SampleFingerprint([string]$Path){
    $fs=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try{
        [long]$len=$fs.Length
        $parts=New-Object 'System.Collections.Generic.List[byte]'
        $lenBytes=[Text.Encoding]::UTF8.GetBytes([string]$len)
        $parts.AddRange($lenBytes)
        $positions=@([long]0,[long][Math]::Max(0,[Math]::Floor($len/2)-2048),[long][Math]::Max(0,$len-4096))|Select-Object -Unique
        foreach($pos in $positions){
            $parts.Add(0)
            [void]$fs.Seek($pos,[IO.SeekOrigin]::Begin)
            $want=[int][Math]::Min(4096,[Math]::Max(0,$len-$pos))
            if($want -gt 0){$buf=New-Object byte[] $want;$got=$fs.Read($buf,0,$want);if($got -gt 0){if($got -lt $want){$buf=$buf[0..($got-1)]};$parts.AddRange($buf)}}
        }
        return Get-IdentityHash $parts.ToArray()
    }finally{$fs.Dispose()}
}
function Write-Utf8NoBom([string]$Path,[string]$Text){[IO.File]::WriteAllText($Path,$Text,[Text.UTF8Encoding]::new($false))}

$kinds=[ordered]@{
    audio='audio';meta='meta';gain='gain';art='artwork';video='video';viz='visualizer'
}
$findings=New-Object 'System.Collections.Generic.List[object]'
$objects=New-Object 'System.Collections.Generic.List[object]'
$stats=[ordered]@{objects=0;bytes=0;receipts=0;missing_receipts=0;repaired=0;mismatches=0;sha256_checked=0;sha256_mismatch=0;sealed_hashes=0;quarantine=0}

function Finding([string]$Severity,[string]$Code,[string]$Message,[object]$Data=$null){$findings.Add([pscustomobject]@{severity=$Severity;code=$Code;message=$Message;data=$Data})}

foreach($pair in $kinds.GetEnumerator()){
    $kind=[string]$pair.Key;$dir=Join-Path $objectRoot ([string]$pair.Value)
    if(-not(Test-Path -LiteralPath $dir)){continue}
    foreach($f in @(Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue)){
        $stats.objects++;$stats.bytes+=$f.Length
        $m=[regex]::Match($f.Name,'^(?<source>[0-9a-fA-F]{16,64})-(?<sig>[0-9a-fA-F]{16})\.(?<ext>.+)$')
        if(-not $m.Success){$stats.mismatches++;Finding 'ERROR' 'OBJECT_NAME' "Object name is outside cache identity grammar: $($f.FullName)";continue}
        $source=$m.Groups['source'].Value.ToLowerInvariant();$sig=$m.Groups['sig'].Value.ToLowerInvariant()
        $receiptPath=Join-Path $receiptRoot ($f.Name+'.receipt.json')
        $sample=Get-SampleFingerprint $f.FullName
        $receipt=$null
        if(Test-Path -LiteralPath $receiptPath){
            $stats.receipts++
            try{$receipt=Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8|ConvertFrom-Json}catch{$stats.mismatches++;Finding 'ERROR' 'RECEIPT_PARSE' "Malformed receipt: $receiptPath" $_.Exception.Message;continue}
        }else{
            $stats.missing_receipts++
            Finding 'WARN' 'UNRECEIPTED' "Object has no commit receipt: $($f.Name)" $kind
            if($RepairReceipts){
                $receipt=[ordered]@{schema=2;kind=$kind;source_key=$source;generation_signature=$sig;object_name=$f.Name;byte_size=[long]$f.Length;sample_fingerprint=$sample;created_unix=[int64]([DateTime]::UtcNow-[DateTime]'1970-01-01').TotalSeconds;repaired=$true}
                Write-Utf8NoBom $receiptPath (($receipt|ConvertTo-Json -Depth 5 -Compress)+"`n")
                $receipt=[pscustomobject]$receipt;$stats.repaired++;$stats.receipts++
            }
        }
        if($receipt){
            $bad=@()
            if([string]$receipt.kind -ne $kind){$bad+='kind'}
            if(([string]$receipt.source_key).ToLowerInvariant() -ne $source){$bad+='source_key'}
            if(([string]$receipt.generation_signature).ToLowerInvariant() -ne $sig){$bad+='generation_signature'}
            if([string]$receipt.object_name -ne $f.Name){$bad+='object_name'}
            if([long]$receipt.byte_size -ne [long]$f.Length){$bad+='byte_size'}
            if([string]$receipt.sample_fingerprint -ne $sample){$bad+='sample_fingerprint'}
            if($bad.Count -gt 0){$stats.mismatches++;Finding 'ERROR' 'RECEIPT_MISMATCH' ("Receipt mismatch {0}: {1}" -f $f.Name,($bad -join ',')) $kind}
            if($DeepHash){
                $sha=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant();$stats.sha256_checked++
                if($receipt.PSObject.Properties['sha256'] -and -not [string]::IsNullOrWhiteSpace([string]$receipt.sha256)){
                    if(([string]$receipt.sha256).ToLowerInvariant() -ne $sha){$stats.sha256_mismatch++;$stats.mismatches++;Finding 'ERROR' 'SHA256_MISMATCH' "SHA-256 mismatch: $($f.Name)" $kind}
                }elseif($SealHashes -and $bad.Count -eq 0){
                    $receipt|Add-Member -NotePropertyName sha256 -NotePropertyValue $sha -Force
                    Write-Utf8NoBom $receiptPath (($receipt|ConvertTo-Json -Depth 6 -Compress)+"`n");$stats.sealed_hashes++
                }
            }
        }
        $objects.Add([pscustomobject]@{kind=$kind;name=$f.Name;bytes=[long]$f.Length;source_key=$source;generation_signature=$sig;sample_fingerprint=$sample})
    }
}

foreach($r in @(Get-ChildItem -LiteralPath $receiptRoot -Filter '*.receipt.json' -File -ErrorAction SilentlyContinue)){
    $name=$r.Name.Substring(0,$r.Name.Length-'.receipt.json'.Length)
    $found=$false
    foreach($dirName in $kinds.Values){if(Test-Path -LiteralPath (Join-Path (Join-Path $objectRoot $dirName) $name)){$found=$true;break}}
    if(-not $found){Finding 'WARN' 'ORPHAN_RECEIPT' "Receipt has no object: $($r.Name)";if($RepairReceipts){Remove-Item -LiteralPath $r.FullName -Force}}
}
$stats.quarantine=@(Get-ChildItem -LiteralPath $quarantineRoot -File -ErrorAction SilentlyContinue).Count

$status=if($stats.mismatches -gt 0){'FAIL'}elseif($stats.missing_receipts -gt 0 -and -not $RepairReceipts){'WARN'}else{'PASS'}
$report=[ordered]@{
    schema=1;product='YOMI';report_type='cache-integrity';generated_utc=[DateTime]::UtcNow.ToString('o');status=$status;
    options=[ordered]@{deep_hash=[bool]$DeepHash;repair_receipts=[bool]$RepairReceipts;seal_hashes=[bool]$SealHashes};
    stats=$stats;findings=$findings.ToArray();objects=$objects.ToArray()
}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$reportPath=Join-Path $reportRoot ("cache-integrity-$stamp.json")
Write-Utf8NoBom $reportPath (($report|ConvertTo-Json -Depth 8)+"`n")
$text=@(
    '===== YOMI CACHE INTEGRITY =====',
    ('STATUS='+$status),
    ('OBJECTS='+$stats.objects),
    ('BYTES='+$stats.bytes),
    ('RECEIPTS='+$stats.receipts),
    ('MISSING_RECEIPTS='+$stats.missing_receipts),
    ('MISMATCHES='+$stats.mismatches),
    ('SHA256_CHECKED='+$stats.sha256_checked),
    ('SHA256_MISMATCH='+$stats.sha256_mismatch),
    ('QUARANTINE='+$stats.quarantine),
    ('REPORT='+$reportPath)
) -join "`r`n"
Write-Host $text
foreach($f in $findings|Select-Object -First 25){Write-Host ("{0,-5} {1}: {2}" -f $f.severity,$f.code,$f.message)}
if($CopyReport){try{$text|Set-Clipboard}catch{}}
if($OpenReport){try{Start-Process notepad.exe ('"'+$reportPath+'"')}catch{}}
if($status -eq 'FAIL'){exit 2};exit 0
