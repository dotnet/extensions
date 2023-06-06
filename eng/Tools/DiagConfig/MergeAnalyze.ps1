#!/usr/bin/env pwsh

param (
    [Parameter(Mandatory = $true)]
    [string] $DotnetToolPath,
    [Parameter(Mandatory = $true)]
    [string] $DiagConfigTool,
    [Parameter(Mandatory = $true)]
    [string] $DiagDefinitionsPath,
    [Parameter(Mandatory = $true)]
    [string] $ReferenedAnalyzers,
    [Parameter(Mandatory = $true)]
    [string] $SdkAnalyzers
)


Write-Output "Processing analyzers`r`n-----------------------------------------------------"
$ReferenedAnalyzers.Split(',') | ForEach-Object {
    $analyzer = $_
    Write-Output "$DotnetToolPath $DiagConfigTool $DiagDefinitionsPath analyzer merge $analyzer";
    Invoke-Expression "$DotnetToolPath $DiagConfigTool $DiagDefinitionsPath analyzer merge $analyzer";
}

$SdkAnalyzers.Split(',') | ForEach-Object {
    $analyzer = $_

    Write-Output "$DotnetToolPath $DiagConfigTool $DiagDefinitionsPath analyzer merge $analyzer";
    Invoke-Expression "$DotnetToolPath $DiagConfigTool $DiagDefinitionsPath analyzer merge $analyzer";
}
