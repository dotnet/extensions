[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArgs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

Import-Module -Scope Local -Force "$PSScriptRoot/scripts/common.psm1"

$script:dotnet = Get-DotNet

Push-Location $PSScriptRoot
try {
    if (Test-Path "$PSScriptRoot/artifacts") {
        Remove-Item -Recurse -Force "$PSScriptRoot/artifacts"
    }
    
    Write-Host -ForegroundColor DarkGray "Executing: dotnet clean $MSBuildArgs"
    Invoke-Block { & $script:dotnet clean @MSBuildArgs }
} finally {
    Pop-Location
}
