#!/usr/bin/env pwsh
<#
.DESCRIPTION
    Builds and invokes the ApiChief tool to generate API review metadata.
.PARAMETER AssemblyPath
    Path to the assembly to extract the API from.
#>

param (
    [Parameter(Mandatory = $true, HelpMessage="Path to the assemly to extract the API from.", Position = 0, ParameterSetName = "AssemblyPath")]
    [string]$AssemblyPath
)

$Project = $PSScriptRoot + "/../eng/Tools/ApiChief/ApiChief.csproj"
$Command = $PSScriptRoot + "/../artifacts/bin/ApiChief/Debug/net8.0/ApiChief.exe"

Write-Output "Building ApiChief tool"

& dotnet build $Project --nologo --verbosity q

Write-Output "Creating API review artifacts in the API.* folder"

& $Command $AssemblyPath emit review
