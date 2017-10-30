#!/usr/bin/env bash

set -euo pipefail
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

RESET="\033[0m"
GREEN="\033[0;32m"
GRAY="\033[0;90m"

#
# Functions
#

__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") [options] [[--] <Arguments>...]"
    echo ""
    echo "Arguments:"
    echo "    <Arguments>...         Arguments passed to MSBuild. Variable number of arguments allowed."
    echo ""
    echo "Options:"
    echo "    --no-test|-NoTest      Skip tests"
    echo ""

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
}

#
# Main
#
notest=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -\?|-h|--help)
            __usage --no-exit
            exit 0
            ;;
        --no-test|-[Nn]o[Tt]est)
            notest=true
            ;;
        --)
            shift
            break
            ;;
        *)
            break
            ;;
    esac
    shift
done

if [ ! -z "${DOTNET_TOOL_DIR:-}" ]; then
    export PATH="${DOTNET_TOOL_DIR}:${PATH}"
fi

pushd "$DIR" >/dev/null

echo -e "${GRAY}MSBuild arguments = $@${RESET}"

dotnet --info

echo -e "${GRAY}Executing: dotnet restore${RESET}"
dotnet restore --force -nologo "$@"

echo -e "${GRAY}Executing: dotnet build${RESET}"
dotnet build --no-restore -nologo "$@"

echo -e "${GRAY}Executing: dotnet pack${RESET}"
dotnet pack --no-build --no-restore -nologo "$@"

if [ $notest != true ]; then
    echo -e "${GRAY}Executing: dotnet test${RESET}"
    dotnet test \
        --no-build --no-restore \
        test/Microsoft.Extensions.CommandLineUtils.Tests/Microsoft.Extensions.CommandLineUtils.Tests.csproj \
        "$@"
else
    echo "Skipping tests because -NoTest was specified"
fi

echo ""
echo -e "${GREEN}Done${RESET}"
echo ""
