#!/usr/bin/env bash

set -e

# Dev Container can run out of disk space if we try to build all TFMs, so only build net8.0
echo "net8.0" > .targetframeworks

# Build the repo
./build.sh

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha