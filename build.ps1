[CmdletBinding()]
param(
    [switch]$NoTest,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArgs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

Import-Module -Scope Local -Force "$PSScriptRoot/scripts/common.psm1"

$script:dotnet = Get-DotNet

Write-Host -ForegroundColor DarkGray "MSBuildArgs = $MSBuildArgs"
& $script:dotnet --info

Push-Location $PSScriptRoot
try {
    Write-Host -ForegroundColor DarkGray "Executing: dotnet restore"
    Invoke-Block { & $script:dotnet restore --force -nologo @MSBuildArgs }

    Write-Host -ForegroundColor DarkGray "Executing: dotnet build"
    Invoke-Block { & $script:dotnet build --no-restore -nologo @MSBuildArgs }

    Write-Host -ForegroundColor DarkGray "Executing: dotnet pack"
    Invoke-Block { & $script:dotnet pack --no-build --no-restore -nologo @MSBuildArgs }

    if (-not $NoTest) {
        Write-Host -ForegroundColor DarkGray "Executing: dotnet test"
        Invoke-Block {
            & $script:dotnet test `
            --no-build `
            --no-restore `
            test/Microsoft.Extensions.CommandLineUtils.Tests/Microsoft.Extensions.CommandLineUtils.Tests.csproj `
            @MSBuildArgs
        }
    }
    else {
        Write-Host "Skipping tests because -NoTest was specified"
    }
    Write-Host -ForegroundColor Green "`nDone`n"
} finally {
    Pop-Location
}
