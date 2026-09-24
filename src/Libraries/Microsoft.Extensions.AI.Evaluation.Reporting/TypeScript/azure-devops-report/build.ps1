param (
    [string]$OutputPath,
    [string]$Version = $null,
    [switch]$IncludeTestPackage = $false
)

$ErrorActionPreference = "Stop"

function Write-NpmFailureDetails {
    param (
        [string]$LogDirectory
    )

    $LogFile = Get-ChildItem -LiteralPath $LogDirectory -Filter "*-debug-0.log" |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $LogFile) {
        return
    }

    $RequestMatch = Select-String -LiteralPath $LogFile.FullName -Pattern "\bhttp fetch GET [45]\d\d (?<Uri>https?://\S+)" |
        Select-Object -Last 1

    if ($null -eq $RequestMatch) {
        return
    }

    $Uri = $RequestMatch.Matches[0].Groups["Uri"].Value
    Write-Host "npm request failed: '$Uri'."

    $PublicRegistry = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/"
    if (-not $Uri.StartsWith($PublicRegistry, [StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    try {
        $Details = Invoke-RestMethod -Uri $Uri -SkipHttpErrorCheck -ConnectionTimeoutSeconds 10 -OperationTimeoutSeconds 10
        if (-not [string]::IsNullOrWhiteSpace($Details.error)) {
            Write-Host "Azure Artifacts response: $($Details.error)"
        }
    }
    catch {
        Write-Warning "Unable to retrieve Azure Artifacts failure details: $_"
    }
}

function Invoke-NativeCommand {
    param (
        [string]$Command,
        [string[]]$Arguments
    )

    $NpmLogDirectory = $null
    if ($Command -in @("npm", "npx")) {
        $NpmCache = & npm config get cache
        if ($LASTEXITCODE -eq 0) {
            $NpmLogDirectory = Join-Path $NpmCache "_logs"
        }
    }

    & $Command @Arguments
    $ExitCode = $LASTEXITCODE

    if ($ExitCode -ne 0 -and $null -ne $NpmLogDirectory) {
        Write-NpmFailureDetails $NpmLogDirectory
    }
    if ($ExitCode -ne 0) {
        throw "'$Command $($Arguments -join ' ')' failed with exit code $ExitCode."
    }
}

function Remove-BuildDirectory {
    param (
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    if (Test-Path -LiteralPath $Path) {
        throw "Failed to remove build directory '$Path'."
    }
}

# if version is not set, then run a script to get it
if ($Version -eq "")
{
    $VSIXPackageVersion = Get-Content $PSScriptRoot/VSIXPackageVersion.json | ConvertFrom-Json
    $Version = $VSIXPackageVersion.PackageVersion
}

$PackageVersion = $Version
$OverrideJson = join-path $PSScriptRoot "override.json"

if ($null -eq $PackageVersion)
{
    Write-Error "Version not set"
    exit 1
}

Write-Host "Using version $PackageVersion"

$TaskPath = Join-Path $PSScriptRoot "tasks/PublishAIEvaluationReport"
Remove-BuildDirectory (Join-Path $TaskPath "node_modules")
Remove-BuildDirectory (Join-Path $TaskPath "dist")
Remove-BuildDirectory (Join-Path $PSScriptRoot "node_modules")
Remove-BuildDirectory (Join-Path $PSScriptRoot "dist")

# Write-Information "Building Report Publishing task"
Set-Location $TaskPath
Invoke-NativeCommand "npm" @("ci", "--omit=dev")
# Copy task files to dist folder
New-Item -ItemType Directory -Path ./dist -Force
copy-item -Path ./task.json -Destination ./dist/ -Force
copy-item -Path ./index.js -Destination ./dist/ -Force
copy-item -Path ./package.json -Destination ./dist/ -Force
copy-item -Path ./node_modules -Destination ./dist/node_modules -Force -Recurse

# remove the test files from resolve package because they are currently breaking vsix signing (zero length)
remove-item -Path ./dist/node_modules/resolve/test -Recurse -Force -ErrorAction SilentlyContinue

@{  version = $PackageVersion
    public = $true } | ConvertTo-Json -Compress | Out-File -FilePath $OverrideJson -Encoding ascii
    
# Write-Information "Building Extension Package" 
Set-Location $PSScriptRoot
Invoke-NativeCommand "npm" @("ci")
Invoke-NativeCommand "npx" @("tsc", "-b")
Invoke-NativeCommand "npx" @("vite", "build")
    
# Copy LICENSE file from the root
copy-item -Path $PSScriptRoot/../../../../../LICENSE -Destination . -Force

Invoke-NativeCommand "npx" @("tfx-cli", "extension", "create", "--overrides-file", $OverrideJson, "--output-path", $OutputPath)

if ($true -eq $IncludeTestPackage) {
    @{  version = $PackageVersion
        id = "microsoft-extensions-ai-evaluation-report-test" 
        name = "[TEST] Azure DevOps AI Evaluation Report" } | ConvertTo-Json -Compress | Out-File -FilePath $OverrideJson -Encoding ascii

    # Build Preview version of the extension for testing
    Invoke-NativeCommand "npx" @("tfx-cli", "extension", "create", "--overrides-file", $OverrideJson, "--output-path", $OutputPath)
}
