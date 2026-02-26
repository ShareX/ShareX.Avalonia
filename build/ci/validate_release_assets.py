#!/usr/bin/env python3

from __future__ import annotations

import argparse
import hashlib
import json
import tarfile
from datetime import datetime, timezone
from pathlib import Path


EXPECTED_ASSETS = [
    ("win", "x64", "exe"),
    ("win", "arm64", "exe"),
    ("mac", "arm64", "tar.gz"),
    ("mac", "x64", "tar.gz"),
    ("linux", "x64", "tar.gz"),
    ("linux", "x64", "deb"),
    ("linux", "x64", "rpm"),
    ("linux", "arm64", "tar.gz"),
    ("linux", "arm64", "deb"),
    ("linux", "arm64", "rpm"),
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Validate release assets for all desktop RIDs and generate metadata manifest."
        )
    )
    parser.add_argument("--assets-dir", required=True, help="Directory containing release files.")
    parser.add_argument("--version", required=True, help="Version string in x.y.z format.")
    parser.add_argument(
        "--metadata-output",
        required=True,
        help="Output JSON path for generated release metadata.",
    )
    return parser.parse_args()


def compute_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)

    return digest.hexdigest()


def get_runtime_id(os_name: str, arch: str) -> str:
    if os_name == "mac":
        return f"osx-{arch}"
    return f"{os_name}-{arch}"


def normalize_tar_name(name: str) -> str:
    normalized = name.replace("\\", "/")
    if normalized.startswith("./"):
        return normalized[2:]
    return normalized


def ensure_tar_has_daemon(path: Path, os_name: str) -> None:
    with tarfile.open(path, "r:gz") as archive:
        names = {normalize_tar_name(member.name) for member in archive.getmembers()}

    if os_name == "linux":
        expected_binary = "xerahs-watchfolder-daemon"
        expected_runtimeconfig = "xerahs-watchfolder-daemon.runtimeconfig.json"
        has_binary = any(name.endswith(expected_binary) for name in names)
        has_runtimeconfig = any(name.endswith(expected_runtimeconfig) for name in names)
        if not has_binary:
            raise RuntimeError(
                f"Missing daemon executable '{expected_binary}' in Linux archive: {path}"
            )

        if not has_runtimeconfig:
            raise RuntimeError(
                f"Missing daemon runtimeconfig '{expected_runtimeconfig}' in Linux archive: {path}"
            )
        return

    if os_name == "mac":
        expected_binary = "XerahS.app/Contents/MacOS/xerahs-watchfolder-daemon"
        expected_runtimeconfig = (
            "XerahS.app/Contents/MacOS/xerahs-watchfolder-daemon.runtimeconfig.json"
        )
        has_binary = any(name.endswith(expected_binary) for name in names)
        has_runtimeconfig = any(name.endswith(expected_runtimeconfig) for name in names)
        if not has_binary:
            raise RuntimeError(
                f"Missing daemon executable '{expected_binary}' in macOS archive: {path}"
            )

        if not has_runtimeconfig:
            raise RuntimeError(
                f"Missing daemon runtimeconfig '{expected_runtimeconfig}' in macOS archive: {path}"
            )


def build_file_name(version: str, os_name: str, arch: str, extension: str) -> str:
    return f"XerahS-{version}-{os_name}-{arch}.{extension}"


def main() -> int:
    args = parse_args()

    assets_dir = Path(args.assets_dir).resolve()
    metadata_path = Path(args.metadata_output).resolve()
    version = args.version

    if not assets_dir.is_dir():
        raise RuntimeError(f"Assets directory does not exist: {assets_dir}")

    metadata = {
        "version": version,
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "assets": [],
    }

    for os_name, arch, extension in EXPECTED_ASSETS:
        file_name = build_file_name(version, os_name, arch, extension)
        file_path = assets_dir / file_name
        if not file_path.is_file():
            raise RuntimeError(f"Missing release asset: {file_path}")

        if extension == "tar.gz":
            ensure_tar_has_daemon(file_path, os_name)

        asset_metadata = {
            "file_name": file_name,
            "runtime_id": get_runtime_id(os_name, arch),
            "os": os_name,
            "arch": arch,
            "package_type": extension,
            "size_bytes": file_path.stat().st_size,
            "sha256": compute_sha256(file_path),
        }
        metadata["assets"].append(asset_metadata)
        print(f"validated {file_name}")

    metadata_path.parent.mkdir(parents=True, exist_ok=True)
    metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    print(f"metadata written to {metadata_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
