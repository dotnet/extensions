#!/usr/bin/env bash

set -euo pipefail
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ ! -z "${DOTNET_TOOL_DIR:-}" ]; then
    export PATH="${DOTNET_TOOL_DIR}:${PATH}"
fi

pushd "$DIR" >/dev/null

if [ -d "artifacts/" ]; then
    rm -r artifacts/
fi

dotnet clean -nologo "$@"
