#!/usr/bin/env bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $2 --channel 2.0 --no-cdn --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$HELIX_CORRELATION_PAYLOAD/sdk/dotnet vstest $1 --logger:trx
