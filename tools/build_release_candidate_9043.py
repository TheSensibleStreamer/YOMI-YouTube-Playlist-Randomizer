#!/usr/bin/env python3
"""Assemble an UNPUBLISHED YOMI release candidate with self-consistent SHA-256 manifests."""
import argparse
import datetime as dt
import hashlib
import json
import shutil
import tempfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ORIGINAL_VERSION = "420.69.9043"
ORIGINAL_BUILD = "R61.106.53.42.1"

def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def json_read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def json_write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def replace_version(path, old, new):
    content = path.read_text(encoding="utf-8-sig")
    count = content.count(old)
    if not count:
        raise RuntimeError(f"Required version marker missing: {path}")
    path.write_text(content.replace(old, new), encoding="utf-8", newline="")
    return count

def create_candidate(version, output):
    if version != "420.69.9043":
        raise ValueError("This script is limited to the staged 9043 candidate; review changes before reuse.")
    output.mkdir(parents=True, exist_ok=True)
    timestamp = dt.datetime.now(dt.timezone.utc).isoformat().replace("+00:00", "Z")
    build_name = "R61.106.53.43.1"

    with tempfile.TemporaryDirectory(prefix="yomi-candidate-") as tmp:
        package = Path(tmp) / "package"
        package.mkdir()
        shutil.copy2(ROOT / "INSTALL YOMI.cmd", package / "INSTALL YOMI.cmd")
        shutil.copytree(ROOT / "installer", package / "installer")
        shutil.copytree(ROOT / "payload", package / "payload")

        app = package / "payload" / "app"
        replace_version(package / "payload" / "VERSION.txt", ORIGINAL_VERSION, version)
        replace_version(app / "FOCUSED-BUILD.txt", ORIGINAL_VERSION, version)
        replace_version(app / "FOCUSED-BUILD.txt", ORIGINAL_BUILD, build_name)
        replace_version(package / "installer" / "install.ps1", ORIGINAL_VERSION, version)

        contract_path = app / "YOMI-CAPABILITY-CONTRACT.json"
        contract = json_read(contract_path)
        assert contract["version"] == ORIGINAL_VERSION, "Unexpected baseline capability version"
        contract["version"] = version
        json_write(contract_path, contract)

        receipt_path = app / "INSTALLATION-RECEIPT.json"
        receipt = json_read(receipt_path)
        assert receipt["version"] == ORIGINAL_VERSION and receipt["status"] == "SEALED"
        receipt["version"] = version
        receipt["generated_utc"] = timestamp
        # This historical field is self-referential: build-manifest hashes this receipt.
        # Never claim its previous hash seals a newly generated manifest.
        receipt.pop("package_manifest_sha256", None)
        json_write(receipt_path, receipt)

        runtime_path = app / "RUNTIME-INTEGRITY-MANIFEST.json"
        runtime = json_read(runtime_path)
        assert runtime["version"] == ORIGINAL_VERSION
        runtime["version"] = version
        runtime["generated_utc"] = timestamp
        # These binaries/HTML files are created by the Windows installer.
        # Their historic hashes cannot validly seal a source-only ZIP.
        generated_on_install = {
            "app/ArtworkEdgeDetector.exe", "app/PriorityRun.exe",
            "app/YomiControllerWpf.exe", "app/YomiLauncher.exe",
            "runtime/mpv/mpv.exe", "runtime/yt-dlp/yt-dlp.exe",
            "web/director.html", "web/overlay.html", "web/visualizer.html",
        }
        listed, actual_missing, sealed = set(), set(), []
        for entry in runtime["files"]:
            rel = entry["path"]
            file = package / "payload" / rel
            if not file.is_file():
                actual_missing.add(rel)
                continue
            entry["bytes"] = file.stat().st_size
            entry["sha256"] = sha256(file)
            sealed.append(entry)
            listed.add(rel)
        if not actual_missing.issubset(generated_on_install):
            raise RuntimeError(
                f"Unexpected source/runtime split: missing={sorted(actual_missing)}"
            )
        runtime["files"] = sealed
        runtime["file_count"] = len(sealed)
        assert len(sealed) == len(listed)
        json_write(runtime_path, runtime)
        print(f"Installer-created runtime files excluded from package seal: {len(actual_missing)}")

        manifest_path = package / "installer" / "build-manifest.json"
        manifest = json_read(manifest_path)
        assert manifest["version"] == "v" + ORIGINAL_VERSION
        manifest["version"] = "v" + version
        manifest["release"] = build_name
        file_list = sorted(
            p for p in package.rglob("*") if p.is_file() and p != manifest_path
        )
        manifest["files"] = {
            p.relative_to(package).as_posix(): {"bytes": p.stat().st_size, "sha256": sha256(p)}
            for p in file_list
        }
        json_write(manifest_path, manifest)

        package_name = f"YOMI-v{version}.zip"
        package_path = output / package_name
        with zipfile.ZipFile(package_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
            for p in sorted(package.rglob("*")):
                if p.is_file():
                    z.write(p, arcname=p.relative_to(package).as_posix())

        # Test the actual bytes of the archive, not just its source directory.
        with zipfile.ZipFile(package_path) as z:
            names = z.namelist()
            if len(names) != len(set(names)) or any(
                item.startswith("/") or "\\" in item or ".." in Path(item).parts for item in names
            ):
                raise RuntimeError("Duplicate or unsafe ZIP member path")
            expect = set(manifest["files"]) | {"installer/build-manifest.json"}
            if set(names) != expect:
                raise RuntimeError(f"ZIP contents differ from manifest: {sorted(set(names) ^ expect)}")
            for name, meta in manifest["files"].items():
                payload = z.read(name)
                if len(payload) != meta["bytes"] or hashlib.sha256(payload).hexdigest() != meta["sha256"]:
                    raise RuntimeError(f"ZIP integrity mismatch: {name}")

        metadata = json_read(ROOT / "update.json")
        metadata["version"] = version
        metadata["package_name"] = package_name
        metadata["package_url"] = (
            "https://raw.githubusercontent.com/TheSensibleStreamer/"
            "YOMI-YouTube-Playlist-Randomizer/main/" + package_name
        )
        metadata["sha256"] = sha256(package_path)
        metadata["summary"] = ""
        json_write(output / "update.json", metadata)

        for name in ["INSTALL YOMI.cmd", "installer/install.ps1",
                     "payload/VERSION.txt", "payload/app/music.lua"]:
            assert (package / name).is_file(), name
        print(f"Candidate only: {package_path.name}")
        print(f"ZIP bytes: {package_path.stat().st_size}")
        print(f"ZIP SHA-256: {metadata['sha256']}")
        print(f"Manifest-verified files: {len(manifest['files'])}")
        print(f"Runtime integrity files: {len(runtime['files'])}")
        print("Unpublished; updater on main is untouched.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", default="420.69.9043")
    parser.add_argument("--output", type=Path, default=Path("dist/9043"))
    args = parser.parse_args()
    create_candidate(args.version, args.output.resolve())
