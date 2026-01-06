#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
png_path="${repo_root}/src/ShareX.Avalonia.UI/Assets/Logo.png"
icns_path="${repo_root}/src/ShareX.Avalonia.UI/Assets/Logo.icns"
csproj_path="${repo_root}/src/ShareX.Avalonia.App/ShareX.Avalonia.App.csproj"

if [[ ! -f "${png_path}" ]]; then
  echo "Logo.png not found at ${png_path}" >&2
  exit 1
fi

if ! command -v sips >/dev/null 2>&1; then
  echo "sips is required but not found." >&2
  exit 1
fi

if ! command -v iconutil >/dev/null 2>&1; then
  echo "iconutil is required but not found." >&2
  exit 1
fi

tmp_dir="$(mktemp -d)"
iconset_dir="${tmp_dir}/ShareX.iconset"
mkdir -p "${iconset_dir}"

sips -z 16 16   "${png_path}" --out "${iconset_dir}/icon_16x16.png" >/dev/null
sips -z 32 32   "${png_path}" --out "${iconset_dir}/icon_16x16@2x.png" >/dev/null
sips -z 32 32   "${png_path}" --out "${iconset_dir}/icon_32x32.png" >/dev/null
sips -z 64 64   "${png_path}" --out "${iconset_dir}/icon_32x32@2x.png" >/dev/null
sips -z 128 128 "${png_path}" --out "${iconset_dir}/icon_128x128.png" >/dev/null
sips -z 256 256 "${png_path}" --out "${iconset_dir}/icon_128x128@2x.png" >/dev/null
sips -z 256 256 "${png_path}" --out "${iconset_dir}/icon_256x256.png" >/dev/null
sips -z 512 512 "${png_path}" --out "${iconset_dir}/icon_256x256@2x.png" >/dev/null
sips -z 512 512 "${png_path}" --out "${iconset_dir}/icon_512x512.png" >/dev/null
sips -z 1024 1024 "${png_path}" --out "${iconset_dir}/icon_512x512@2x.png" >/dev/null

iconutil -c icns "${iconset_dir}" -o "${icns_path}"

python3 - <<'PY'
from pathlib import Path
import re

csproj_path = Path("""${csproj_path}""")
text = csproj_path.read_text()

if "Logo.icns" in text and "ApplicationIcon" in text:
    print("csproj already references Logo.icns; skipping csproj update")
else:
    text = re.sub(r"^\s*<ApplicationIcon>.*?</ApplicationIcon>\s*\n", "", text, flags=re.MULTILINE)
    block = (
        "  <PropertyGroup Condition=\"'$(OS)' == 'Windows_NT'\">\n"
        "    <ApplicationIcon>..\\ShareX.Avalonia.UI\\Assets\\Logo.ico</ApplicationIcon>\n"
        "  </PropertyGroup>\n"
        "  <PropertyGroup Condition=\"'$(OS)' != 'Windows_NT'\">\n"
        "    <ApplicationIcon>..\\ShareX.Avalonia.UI\\Assets\\Logo.icns</ApplicationIcon>\n"
        "  </PropertyGroup>\n"
    )

    insert_after = text.find("</PropertyGroup>")
    if insert_after != -1:
        insert_after = insert_after + len("</PropertyGroup>")
        text = text[:insert_after] + "\n" + block + text[insert_after:]
    else:
        text = text + "\n" + block

    csproj_path.write_text(text)
    print("Updated csproj to use Logo.icns on macOS")

PY

rm -rf "${tmp_dir}"

printf "Done. icns: %s\n" "${icns_path}"
