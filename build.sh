#!/usr/bin/env bash

function is_cygwin_or_mingw()
{
  case $(uname -s) in
    CYGWIN*)    return 0;;
    MINGW*)     return 0;;
    *)          return 1;;
  esac
}

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

if is_cygwin_or_mingw; then
  # if bash shell running on Windows (not WSL),
  # pass control to powershell build script.
  DIR=$(cygpath -d "$DIR")
  powershell -c "$DIR\\build.cmd" $@
else
  "$DIR/eng/build.sh" $@
fi
