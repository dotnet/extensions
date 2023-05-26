#!/usr/bin/env pwsh
<#
.DESCRIPTION
    Creates API baseline files representing the current API surface exposed by this repo.
#>

$Project = $PSScriptRoot + "/../eng/Tools/ApiChief/ApiChief.csproj"
$Command = $PSScriptRoot + "/../artifacts/bin/ApiChief/Debug/net8.0/ApiChief.exe"

Write-Output "Building ApiChief tool"

& dotnet build $Project --nologo --verbosity q

Write-Output "Creating API baseline files in the src/Libraries folder"

Get-ChildItem -Path src/Libraries -Depth 1 -Include *.csproj | ForEach-Object `
{
    $name = Split-Path $_.FullName -LeafBase
    $path = "$PSScriptRoot\..\artifacts\bin\$name\Debug\net8.0\$name.dll"
    Write-Host "  Processing" $name
    & $Command $path emit baseline -o "src/Libraries/$name/$name.json"
}
