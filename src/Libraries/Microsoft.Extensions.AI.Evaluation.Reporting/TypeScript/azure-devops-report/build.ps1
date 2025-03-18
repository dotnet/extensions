param (
    [string]$OutputPath,
    [string]$Version = $null,
    [bool]$IncludeTestPackage = $false
)

# if version is not set, then run a script to get it
if ($Version -eq "")
{
    $VSIXPackageVersion = Get-Content $PSScriptRoot/VSIXPackageVersion.json | ConvertFrom-Json
    $Version = $VSIXPackageVersion.PackageVersion
}

$PackageVersion = $Version

if ($null -eq $PackageVersion)
{
    Write-Error "Version not set"
    exit 1
}

Write-Host "Using version $PackageVersion"

# Write-Information "Building Report Publishing task"
Push-Location $PSScriptRoot/tasks/PublishAIEvaluationReport
npm install --omit=dev
# Copy task files to dist folder
New-Item -ItemType Directory -Path ./dist -Force
copy-item -Path ./task.json -Destination ./dist/ -Force
copy-item -Path ./index.js -Destination ./dist/ -Force
copy-item -Path ./package.json -Destination ./dist/ -Force
copy-item -Path ./node_modules -Destination ./dist/node_modules -Force -Recurse

# remove the test files from resolve package because they are currently breaking vsix signing (zero length)
remove-item -Path ./dist/node_modules/resolve/test -Recurse -Force

@{  version = $PackageVersion
    public = $true } | ConvertTo-Json -Compress | Out-File -FilePath $PSScriptRoot/override.json

# Write-Information "Building Extension Package" 
Set-Location $PSScriptRoot
npm install
npx tsc -b
npx vite build

npx tfx-cli extension create --overrides-file $PSScriptRoot/override.json --output-path $OutputPath

if ($true -eq $IncludeTestPackage) {
    @{  version = $PackageVersion
        id = "microsoft-extensions-ai-evaluation-report-test" 
        name = "[TEST] Azure DevOps AI Evaluation Report" } | ConvertTo-Json -Compress | Out-File -FilePath $PSScriptRoot/override.json

    # Build Preview version of the extension for testing
    npx tfx-cli extension create --overrides-file $PSScriptRoot/override.json --output-path $OutputPath
}