param (
    [switch]$Configure=$False,
    [switch]$Unconfigure=$False,
    [string]$ConfigRoot=$Null
)

Write-Host "$PSScriptRoot"

if ($Configure -and $Unconfigure) {
    Write-Error -Message "Cannot specify both -Configure and -Unconfigure"
    Exit 1
}

if ($ConfigRoot -eq $Null) {
    $ConfigRoot = "$HOME/.config/dotnet-extensions"
}

$ProjectRoot = Resolve-Path "$PSScriptRoot/../test/Libraries"
$ReportingConfig = "Microsoft.Extensions.AI.Evaluation.Reporting.Tests/appsettings.local.json"
$IntegrationConfig = "Microsoft.Extensions.AI.Evaluation.Integration.Tests/appsettings.local.json"

if ($Configure) {
    if (!(Test-Path -Path "$ConfigRoot/$ReportingConfig")) {
        Write-Host "No configuration found at $ConfigRoot/$ReportingConfig"
        Exit 0
    }
    if (!(Test-Path -Path "$ConfigRoot/$IntegrationConfig")) {
        Write-Host "No configuration found at $ConfigRoot/$IntegrationConfig"
        Exit 0
    }

    Copy-Item -Path "$ConfigRoot/$ReportingConfig" -Destination "$ProjectRoot/$ReportingConfig" -Force
    Copy-Item -Path "$ConfigRoot/$IntegrationConfig" -Destination "$ProjectRoot/$IntegrationConfig" -Force

    Write-Host "Test configured to use external resources"
} elseif ($Unconfigure) {
    Remove-Item -Path "$ProjectRoot/$ReportingConfig" -Force
    Remove-Item -Path "$ProjectRoot/$IntegrationConfig" -Force

    Write-Host "Test unconfigured from using external resources"
} else {
    Write-Error -Message "Must specify either -Configure or -Unconfigure"
    Exit 1
}

