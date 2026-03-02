#!/bin/bash
set -euo pipefail

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ROOT="$SCRIPT_DIR/../.."
PROJECT="$ROOT/src/desktop/app/XerahS.App/XerahS.App.csproj"
OUTPUT_DIR="$ROOT/dist"

if [ ! -d "$OUTPUT_DIR" ]; then
    mkdir -p "$OUTPUT_DIR"
fi

# Get Version from Directory.Build.props
VERSION=$(grep '<Version>' "$ROOT/Directory.Build.props" | sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' | tr -d '[:space:]')
echo "Building XerahS version $VERSION for Linux..."

dotnet_publish_serial() {
    dotnet publish "$@" \
        --disable-build-servers \
        -p:nodeReuse=false \
        -p:UseSharedCompilation=false \
        -p:BuildInParallel=false \
        -m:1
}

publish_single_plugin() {
    local plugin_project="$1"
    local plugins_dir="$2"
    local publish_dir="$3"
    local arch="$4"

    local plugin_dir plugin_name plugin_id plugin_output id_match
    plugin_dir=$(dirname "$plugin_project")
    plugin_name=$(basename "$plugin_project" .csproj)
    plugin_id="$plugin_name"

    # Determine plugin ID from plugin.json when available.
    if [ -f "$plugin_dir/plugin.json" ]; then
        id_match=$(grep -o '"pluginId"[[:space:]]*:[[:space:]]*"[^"]*"' "$plugin_dir/plugin.json" | cut -d'"' -f4 || true)
        if [ -n "${id_match:-}" ]; then
            plugin_id="$id_match"
        fi
    fi

    echo "  Publishing Plugin: $plugin_name ($plugin_id) for $arch"
    plugin_output="$plugins_dir/$plugin_id"
    rm -rf "$plugin_output"
    mkdir -p "$plugin_output"

    dotnet_publish_serial "$plugin_project" \
        -c Release \
        -r "$arch" \
        -p:OS=Linux \
        -o "$plugin_output" \
        --no-self-contained \
        -p:PublishSingleFile=false \
        -p:EnableWindowsTargeting=true > /dev/null

    # Ensure plugin.json exists for runtime discovery.
    if [ ! -f "$plugin_output/plugin.json" ] && [ -f "$plugin_dir/plugin.json" ]; then
        cp "$plugin_dir/plugin.json" "$plugin_output/plugin.json"
    fi

    # Cleanup: remove files that already exist in the main app directory.
    local f fname
    for f in "$plugin_output"/*; do
        if [ -f "$f" ]; then
            fname=$(basename "$f")
            if [ -f "$publish_dir/$fname" ]; then
                rm "$f"
            fi
        fi
    done

    if [ ! -f "$plugin_output/plugin.json" ]; then
        echo "Error: plugin.json missing for plugin '$plugin_id' in $plugin_output" >&2
        return 1
    fi
}

validate_daemon_bundle() {
    local publish_dir="$1"
    local daemon_path="$publish_dir/xerahs-watchfolder-daemon"
    local runtimeconfig_path="$publish_dir/xerahs-watchfolder-daemon.runtimeconfig.json"

    if [ ! -f "$daemon_path" ]; then
        echo "Error: Missing daemon executable in publish output: $daemon_path"
        exit 1
    fi

    if [ ! -f "$runtimeconfig_path" ]; then
        echo "Error: Missing daemon runtimeconfig in publish output: $runtimeconfig_path"
        exit 1
    fi
}

# Define Architectures to Build
# Override with XERAHS_ARCHITECTURES, e.g. "linux-x64" or "linux-arm64".
if [ -n "${XERAHS_ARCHITECTURES:-}" ]; then
    IFS=',' read -r -a ARCHITECTURES <<< "$XERAHS_ARCHITECTURES"
    for i in "${!ARCHITECTURES[@]}"; do
        ARCHITECTURES[$i]="${ARCHITECTURES[$i]//[[:space:]]/}"
    done
else
    ARCHITECTURES=("linux-x64" "linux-arm64")
fi

for ARCH in "${ARCHITECTURES[@]}"; do
    echo ""
    echo "=========================================="
    echo "Building for Architecture: $ARCH"
    echo "=========================================="
    
    # 1. Clean & Publish
    PUBLISH_DIR="$ROOT/src/desktop/app/XerahS.App/bin/Release/net10.0/$ARCH/publish"
    
    if [ -d "$PUBLISH_DIR" ]; then
        rm -rf "$PUBLISH_DIR"
    fi

    echo "Running dotnet publish ($ARCH)..."
    dotnet build-server shutdown >/dev/null 2>&1 || true
    dotnet_publish_serial "$PROJECT" \
        -c Release \
        -r "$ARCH" \
        -p:OS=Linux \
        -p:DefineConstants=LINUX \
        -p:PublishSingleFile=true \
        --self-contained true \
        -p:EnableWindowsTargeting=true \
        -p:SkipBundlePlugins=true

    validate_daemon_bundle "$PUBLISH_DIR"

    # 1.5 Publish Plugins
    echo "Publishing Plugins ($ARCH)..."
    PLUGINS_DIR="$PUBLISH_DIR/Plugins"
    mkdir -p "$PLUGINS_DIR"

    mapfile -d '' -t PLUGIN_PROJECTS < <(find "$ROOT/src/desktop/plugins" -mindepth 2 -maxdepth 2 -name "*.csproj" -print0 | sort -z)
    PLUGIN_COUNT="${#PLUGIN_PROJECTS[@]}"
    if [ "$PLUGIN_COUNT" -eq 0 ]; then
        echo "Error: No plugins were published for $ARCH."
        exit 1
    fi

    # Publish plugin projects in parallel; override with XERAHS_PLUGIN_JOBS.
    PLUGIN_JOBS="${XERAHS_PLUGIN_JOBS:-4}"
    if ! [[ "$PLUGIN_JOBS" =~ ^[1-9][0-9]*$ ]]; then
        echo "Error: XERAHS_PLUGIN_JOBS must be a positive integer (received '$PLUGIN_JOBS')."
        exit 1
    fi

    export PLUGINS_DIR PUBLISH_DIR ARCH
    export -f dotnet_publish_serial
    export -f publish_single_plugin

    printf '%s\0' "${PLUGIN_PROJECTS[@]}" | xargs -0 -n1 -P "$PLUGIN_JOBS" bash -c '
        publish_single_plugin "$1" "$PLUGINS_DIR" "$PUBLISH_DIR" "$ARCH"
    ' _

    if ! find "$PLUGINS_DIR" -mindepth 2 -maxdepth 2 -name "plugin.json" | grep -q .; then
        echo "Error: No plugin manifests found under $PLUGINS_DIR after publish."
        exit 1
    fi

    echo "Published $PLUGIN_COUNT plugins to startup Plugins folder: $PLUGINS_DIR"
    dotnet build-server shutdown >/dev/null 2>&1 || true

    # 2. Package
    echo "Packaging ($ARCH)..."
    echo "Note: rpmbuild is required to produce RPM packages."
    PACKAGING_TOOL="$ROOT/build/linux/XerahS.Packaging/XerahS.Packaging.csproj"
    dotnet run --project "$PACKAGING_TOOL" -- "$PUBLISH_DIR" "$OUTPUT_DIR" "$VERSION" "$ARCH"
done

echo ""
echo "Done! All packages in $OUTPUT_DIR"
