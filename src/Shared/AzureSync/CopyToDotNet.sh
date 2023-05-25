#!/usr/bin/env bash

if [[ -n "$1" ]]; then
    remote_repo="$1"
else
    remote_repo="$DOTNETEXTENSIONS_REPO"
fi

if [[ -z "$remote_repo" ]]; then
    echo The 'DOTNETEXTENSIONS_REPO' environment variable or command line parameter is not set, aborting.
    exit 1
fi

cd "$(dirname "$0")" || exit 1

echo "DOTNETEXTENSIONS_REPO: $remote_repo"

rsync -av --delete ../Data.Validation/ "$remote_repo"/src/Shared/Data.Validation/
rsync -av --delete ../DotNetSync/ "$remote_repo"/src/Shared/AzureSync/
rsync -av --delete ../EmptyCollections/ "$remote_repo"/src/Shared/EmptyCollections/
rsync -av --delete ../NumericExtensions/ "$remote_repo"/src/Shared/NumericExtensions/
rsync -av --delete ../Throw/ "$remote_repo"/src/Shared/Throw/
