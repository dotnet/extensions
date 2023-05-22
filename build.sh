#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

set -euo pipefail

if [[ $# < 1 ]]
then
    # Perform restore and build, if no args are supplied.
    set -- --restore --build;
fi

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
"$DIR/eng/build.sh" "$@"
