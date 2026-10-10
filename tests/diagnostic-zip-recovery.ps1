# Run in Windows PowerShell 5.1, using the real diagnostic ZIP implementation.
$ErrorActionPreference='Stop'
$code=Get-Content 'payload/app/YomiDiagnosticBundle.ps1' -Raw
$begin=$code.IndexOf('# Create a real, readable ZIP.')
$end=$code.IndexOf("`nif ($zipValid) {",$begin)
if($begin -lt 0 -or $end -le $begin){throw 'Missing production ZIP implementation'}
$implementation=$code.Substring($begin,$end-$begin)
$script=[scriptblock]::Create($implementation)
$workspace=Join-Path $env:TEMP ('YOMI-ZIP-CONTRACT-'+[Guid]::NewGuid().ToString('N'))
$out=Join-Path $workspace 'YOMI-Diagnostics-test'
$zip=$out+'.zip'
New-Item -ItemType Directory -Path $out -Force | Out-Null
try {
    'Critical update-state file included' | Set-Content -Path (Join-Path $out '00-summary.txt')
    $sub=Join-Path $out 'Runtime-Truth'
    New-Item -ItemType Directory -Path $sub -Force | Out-Null
    '{"state":"INSTALLING"}' | Set-Content -LiteralPath (Join-Path $sub 'update-transaction.json')
    'This file is deliberately locked' | Set-Content -LiteralPath (Join-Path $sub 'locked.log')
    $locked=[System.IO.File]::Open((Join-Path $sub 'locked.log'), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        . $script
    } finally {
        $locked.Dispose()
    }
    if(-not $zipValid){throw ('Diagnostic ZIP failed recovery: '+$zipError)}
    if(-not (Test-Path -LiteralPath $zip)){throw 'Validated ZIP does not exist'}
    $archive=[IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $names=@($archive.Entries|ForEach-Object {$_.FullName})
        if($names -notcontains '00-summary.txt'){throw 'Critical summary was not archived'}
        if($names -notcontains 'Runtime-Truth/update-transaction.json'){throw 'Updater state was not archived'}
        if($names -notcontains 'ZIP-OMITTED-FILES.txt'){throw 'Locked-file recovery not logged'}
    } finally {
        $archive.Dispose()
    }
    # A non-ZIP file may not be marked as successfully validated.
    [IO.File]::WriteAllText((Join-Path $workspace 'bad.zip'),'not a zip archive')
    $bad=Test-YomiDiagnosticZip (Join-Path $workspace 'bad.zip')
    if($bad.Valid){throw 'Invalid ZIP passed validation'}
    Write-Host 'PASS: source diagnostic ZIP recovers locked file, retains updater evidence and rejects corrupt archives'
} finally {
    Remove-Item -LiteralPath $workspace -Recurse -Force -ErrorAction SilentlyContinue
}
