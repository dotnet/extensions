# This script needs to be run on PowerShell 7+ (for ConvertFrom-Json) in the directory of the project

param (
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path .\package.json)) {
    Write-Error "package.json not found in the current directory. Please run this script in the directory of the project."
    exit 1
}

try {
    Get-Command artifacts-npm-credprovider | Out-Null
    Write-Host "artifacts-npm-credprovider is already installed"
}
catch {
    Write-Host "Installing artifacts-npm-credprovider"
    if (-not $WhatIf) {
        npm install -g @microsoft/artifacts-npm-credprovider --registry https://pkgs.dev.azure.com/artifacts-public/23934c1b-a3b5-4b70-9dd3-d1bef4cc72a0/_packaging/AzureArtifacts/npm/registry/
    }
}

Write-Host "Provisioning a token for the NPM registry. You might be prompted to authenticate."
if (-not $WhatIf) {
    # This command provisions a token for the AzDO NPM registry to run npm install and ensure any missing package is mirrored.
    artifacts-npm-credprovider -f -c .\.npmrc
}

Write-Host "Running npm install"
if (-not $WhatIf) {
    npm install --prefer-online --include optional
}

# Add optional dependencies to the cache to ensure that they get mirrored
Write-Host "Adding optional dependencies to the cache"
$packages = (Get-Content .\package-lock.json | ConvertFrom-Json -AsHashtable).packages

$allOptionalDependencies = @()
foreach ($packagePath in $packages.Keys) {
    $package = $packages[$packagePath]
    if ($null -eq $package.optionalDependencies) {
        continue
    }

    $optionalDependencies = $package.optionalDependencies
    foreach ($optionalDependencyName in $optionalDependencies.Keys) {
        $optionalDependencyVersion = $optionalDependencies[$optionalDependencyName]
        $allOptionalDependencies += "$optionalDependencyName@$optionalDependencyVersion"
    }
}

if ($allOptionalDependencies.Count -gt 0) {
    Write-Host "Found $($allOptionalDependencies.Count) optional dependencies:"
    $allOptionalDependencies | ForEach-Object { Write-Host "  $_" }
    if (-not $WhatIf) {
        npm cache add @allOptionalDependencies
    }
}
