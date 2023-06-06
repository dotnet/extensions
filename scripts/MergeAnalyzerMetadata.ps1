#!/usr/bin/env pwsh
<#
.DESCRIPTION
    Builds and invokes the DiagConfig tool to extract metadata from analyzers and update our diagnostic config state accordingly.
#>


Set-Location $PSScriptRoot/..

if ($IsWindows) {
    .\build.cmd -restore -build -projects .\eng\Tools\DiagConfig\DiagConfig.csproj -bl
}
else {
    ./build.sh --restore --build --projects ./eng/Tools/DiagConfig/DiagConfig.csproj --bl
}