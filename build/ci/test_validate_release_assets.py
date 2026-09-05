#!/usr/bin/env python3
"""Archive fixtures for portable release validation; no release downloads required."""

import json
import tempfile
import unittest
import zipfile
from pathlib import Path

from validate_release_assets import (
    EXPECTED_ASSETS,
    build_file_name,
    ensure_portable_zip_payload,
)


class PortableReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_dir.cleanup)
        self.path = Path(self.temp_dir.name) / "portable.zip"
        self.payload = {
            "XerahS.exe": b"app",
            "xerahs-watchfolder-daemon.exe": b"daemon",
            "portable.txt": b"",
            "coreclr.dll": b"runtime",
            "LICENSE.txt": b"license",
            "frontend/dist/index.html": b"<html>editor</html>",
            "Plugins/example/plugin.json": json.dumps({"assemblyFileName": "Example.dll"}).encode(),
            "Plugins/example/Example.dll": b"plugin",
        }

    def write_archive(self):
        with zipfile.ZipFile(self.path, "w", zipfile.ZIP_DEFLATED) as archive:
            for name, content in self.payload.items():
                archive.writestr(name, content)

    def test_both_architectures_are_required_with_matching_names(self):
        for arch in ("x64", "arm64"):
            self.assertIn(("win", arch, "portable.zip"), EXPECTED_ASSETS)
            self.assertEqual(
                build_file_name("1.2.3", "win", arch, "portable.zip"),
                f"XerahS-1.2.3-win-{arch}-portable.zip",
            )
        self.assertEqual(build_file_name("1.2.3", "win", "x64", "exe"), "XerahS-1.2.3-win-x64.exe")

    def test_valid_archive_allows_empty_marker(self):
        self.write_archive()
        ensure_portable_zip_payload(self.path)

    def test_required_payload_cannot_be_missing(self):
        for name in ("XerahS.exe", "portable.txt", "xerahs-watchfolder-daemon.exe", "frontend/dist/index.html"):
            with self.subTest(name=name):
                content = self.payload.pop(name)
                self.write_archive()
                with self.assertRaisesRegex(RuntimeError, "Missing portable payload"):
                    ensure_portable_zip_payload(self.path)
                self.payload[name] = content

    def test_wrapping_directory_is_rejected(self):
        self.payload = {"XerahS/" + name: value for name, value in self.payload.items()}
        self.write_archive()
        with self.assertRaisesRegex(RuntimeError, "Missing portable payload"):
            ensure_portable_zip_payload(self.path)

    def test_declared_plugin_assembly_must_be_present(self):
        del self.payload["Plugins/example/Example.dll"]
        self.write_archive()
        with self.assertRaisesRegex(RuntimeError, "Missing or empty plugin assembly"):
            ensure_portable_zip_payload(self.path)

    def test_plugin_free_archive_is_rejected(self):
        self.payload = {name: value for name, value in self.payload.items() if not name.startswith("Plugins/")}
        self.write_archive()
        with self.assertRaisesRegex(RuntimeError, "No plugin manifests"):
            ensure_portable_zip_payload(self.path)

    def test_empty_executable_is_rejected(self):
        self.payload["XerahS.exe"] = b""
        self.write_archive()
        with self.assertRaisesRegex(RuntimeError, "Empty portable payload"):
            ensure_portable_zip_payload(self.path)

    def test_symbols_are_rejected_even_inside_plugins(self):
        self.payload["Plugins/example/Example.PDB"] = b"symbols"
        self.write_archive()
        with self.assertRaisesRegex(RuntimeError, "Debug symbols"):
            ensure_portable_zip_payload(self.path)

    def test_unsafe_member_paths_are_rejected(self):
        for name in ("../escape.dll", "/absolute.dll", "C:/drive.dll"):
            with self.subTest(name=name):
                self.payload[name] = b"bad"
                self.write_archive()
                with self.assertRaisesRegex(RuntimeError, "Unsafe portable archive path"):
                    ensure_portable_zip_payload(self.path)
                del self.payload[name]


if __name__ == "__main__":
    unittest.main()
