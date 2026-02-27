#!/usr/bin/env bash
set -eu

DOTNET_BIN="$(command -v dotnet || true)"

if [ -z "$DOTNET_BIN" ] && [ -x "$HOME/.dotnet/dotnet" ]; then
	DOTNET_BIN="$HOME/.dotnet/dotnet"
fi

if [ -z "$DOTNET_BIN" ]; then
	echo "Error: dotnet not found in PATH or at $HOME/.dotnet/dotnet" >&2
	exit 1
fi

"$DOTNET_BIN" run --project XerahS.App/XerahS.App.csproj -c Debug "$@"
