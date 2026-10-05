from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.2"
PREVIOUS = "4.2.0.9.1"
REVISION = "R61.106.52.13.8"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else f"YOMI-v{VERSION}.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

def replace_once(path, old, new):
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one anchor, got {count}: {old[:100]!r}")
    write(path, text.replace(old, new, 1))

replace_once(
    "payload/app/YomiLauncher.cs",
    "            psi.WorkingDirectory = appDir;",
    """            string launchWorkingDirectory = appDir;
            if (updateMode)
            {
                launchWorkingDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "YOMI", "updates"
                );
                Directory.CreateDirectory(launchWorkingDirectory);
            }
            psi.WorkingDirectory = launchWorkingDirectory;"""
)

replace_once(
    "payload/app/update.ps1",
    "New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force|Out-Null",
    """New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force|Out-Null
# Never keep Program Files\\YOMI\\app as this process's current directory while an update
# replaces the installation tree. This prevents the updater itself from locking the app folder.
try {
    [Environment]::CurrentDirectory = $updateRoot
    Set-Location -LiteralPath $updateRoot
} catch {}"""
)

installer = read("installer/install.ps1")
anchor = """    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {"""
bootstrap = """    # Bootstrap lock escape for 4.2.0.9/4.2.0.9.1.
    # Those builds can leave update.ps1 alive with Program Files\\YOMI\\app as its process CWD,
    # which prevents the installation directory from being atomically renamed.
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ProcessId -ne $installerPid -and
            $_.CommandLine -and
            (
                $_.CommandLine -like "*$installRoot\\app\\update.ps1*" -or
                $_.CommandLine -like "*$installRoot\\app\\update-deployment.ps1*" -or
                $_.CommandLine -like "*$installRoot\\app\\YomiUpdateHost.ps1*"
            )
        } |
        ForEach-Object {
            Write-Host ("      Releasing old updater lock PID " + $_.ProcessId + " " + $_.Name) -ForegroundColor DarkGray
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Milliseconds 350

    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {"""
if installer.count(anchor) != 1:
    raise SystemExit("installer process-sweep anchor missing or duplicated")
installer = installer.replace(anchor, bootstrap, 1)

old = """                        $_.CommandLine -like "*C:\\Program Files\\YOMI\\app\\playlist-refresh.ps1*" -or
                        $_.CommandLine -like "*yomi-rc2*" -or"""
new = """                        $_.CommandLine -like "*C:\\Program Files\\YOMI\\app\\playlist-refresh.ps1*" -or
                        $_.CommandLine -like "*C:\\Program Files\\YOMI\\app\\update.ps1*" -or
                        $_.CommandLine -like "*C:\\Program Files\\YOMI\\app\\update-deployment.ps1*" -or
                        $_.CommandLine -like "*C:\\Program Files\\YOMI\\app\\YomiUpdateHost.ps1*" -or
                        $_.CommandLine -like "*yomi-rc2*" -or"""
if installer.count(old) != 1:
    raise SystemExit("installer normal sweep anchor missing")
installer = installer.replace(old, new, 1)

old_swap = """    if (Test-Path $installRoot) {
        $old = "$installRoot.old"
        Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        Move-Item $installRoot $old -Force
        try {
            Move-Item $stage $installRoot -Force
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            if (Test-Path $installRoot) { Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue }
            Move-Item $old $installRoot -Force
            throw
        }
    }
    else {
        Move-Item $stage $installRoot -Force
    }"""
new_swap = """    if (Test-Path $installRoot) {
        $old = "$installRoot.old"
        if (Test-Path $old) {
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path $old) {
            $old = "$installRoot.old-" + [Guid]::NewGuid().ToString('N')
        }

        $movedOld = $false
        $lastMoveError = $null
        for ($attempt = 1; $attempt -le 20; $attempt++) {
            try {
                [IO.Directory]::Move($installRoot, $old)
                $movedOld = $true
                break
            }
            catch {
                $lastMoveError = $_
                Start-Sleep -Milliseconds 250
            }
        }
        if (-not $movedOld) {
            throw $lastMoveError
        }

        try {
            Move-Item $stage $installRoot -Force
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            if (Test-Path $installRoot) {
                Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $old) {
                [IO.Directory]::Move($old, $installRoot)
            }
            throw
        }
    }
    else {
        Move-Item $stage $installRoot -Force
    }"""
if installer.count(old_swap) != 1:
    raise SystemExit("installer atomic swap anchor missing")
installer = installer.replace(old_swap, new_swap, 1)

verify_anchor = """    if ($installDeno -and -not (Test-Path (Join-Path $installRoot 'runtime\\deno\\deno.exe'))) { throw 'Final verification failed: Deno was selected but deno.exe is missing.' }


    Write-Host ''"""
verify_new = """    if ($installDeno -and -not (Test-Path (Join-Path $installRoot 'runtime\\deno\\deno.exe'))) { throw 'Final verification failed: Deno was selected but deno.exe is missing.' }

    Write-Host '      Verifying current WPF control plane...' -ForegroundColor DarkCyan
    $controllerSelfTest = Start-Process -FilePath (Join-Path $installRoot 'app\\YomiControllerWpf.exe') -ArgumentList '--self-test' -WorkingDirectory (Join-Path $installRoot 'app') -PassThru -Wait
    if ($controllerSelfTest.ExitCode -ne 0) {
        throw ('Final verification failed: YomiControllerWpf self-test exit ' + $controllerSelfTest.ExitCode)
    }

    # If this install bootstrapped itself by terminating the old updater, close the transaction here.
    $updateTxFile = Join-Path $dataRoot 'state\\update-transaction.json'
    if (Test-Path $updateTxFile) {
        try {
            $updateTx = Get-Content $updateTxFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($updateTx -and [string]$updateTx.to_version -eq '4.2.0.9.2') {
                $updateTx.state = 'COMPLETE'
                $updateTx.reason = 'installer-verified-control-plane-after-bootstrap-lock-release'
                $updateTx.updated_utc = [DateTime]::UtcNow.ToString('o')
                [IO.File]::WriteAllText(
                    $updateTxFile,
                    ($updateTx | ConvertTo-Json -Depth 12),
                    [Text.UTF8Encoding]::new($false)
                )
            }
        }
        catch {}
    }


    Write-Host ''"""
if installer.count(verify_anchor) != 1:
    raise SystemExit("installer final verification anchor missing")
installer = installer.replace(verify_anchor, verify_new, 1)
write("installer/install.ps1", installer)

text_exts = {".ps1", ".cs", ".json", ".txt", ".xaml", ".cmd", ".config", ".manifest", ".md"}
changed = 0
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name == "build-manifest.json" or p.suffix.lower() not in text_exts:
        continue
    try:
        t = p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        continue
    if PREVIOUS in t:
        p.write_text(t.replace(PREVIOUS, VERSION), encoding="utf-8", newline="")
        changed += 1

write("payload/VERSION.txt", VERSION + "\n")
write("payload/app/FOCUSED-BUILD.txt", f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

launcher = read("payload/app/YomiLauncher.cs")
updater = read("payload/app/update.ps1")
installer = read("installer/install.ps1")
assert "launchWorkingDirectory" in launcher and '"YOMI", "updates"' in launcher
assert "[Environment]::CurrentDirectory = $updateRoot" in updater
assert "Releasing old updater lock PID" in installer
assert "[IO.Directory]::Move($installRoot, $old)" in installer
assert "Verifying current WPF control plane" in installer
assert read("payload/VERSION.txt").strip() == VERSION

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

files = {}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file():
        continue
    rel = p.relative_to(ROOT).as_posix()
    if rel == "installer/build-manifest.json":
        continue
    files[rel] = {"bytes": p.stat().st_size, "sha256": sha256(p)}

manifest = {
    "version": "v" + VERSION,
    "product": "YOMI - YouTube OBS Music Interface",
    "release": REVISION,
    "files": files,
}
(ROOT / "installer/build-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="")

actual = {p.relative_to(ROOT).as_posix() for p in ROOT.rglob("*")
          if p.is_file() and p.relative_to(ROOT).as_posix() != "installer/build-manifest.json"}
if actual != set(files):
    raise SystemExit("manifest file set mismatch")
for rel, meta in files.items():
    p = ROOT / rel
    if p.stat().st_size != meta["bytes"] or sha256(p) != meta["sha256"]:
        raise SystemExit("manifest verification failed: " + rel)

if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file():
            z.write(p, p.relative_to(ROOT).as_posix())

print(json.dumps({
    "version": VERSION,
    "revision": REVISION,
    "changed_identity_files": changed,
    "bytes": OUT.stat().st_size,
    "sha256": sha256(OUT),
    "files": len(files) + 1
}, indent=2))
