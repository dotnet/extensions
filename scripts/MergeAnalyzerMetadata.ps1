#!/usr/bin/env pwsh
<#
.DESCRIPTION
    Builds and invokes the DiagConfig tool to extract metadata from analyzers and update our diagnostic config state accordingly.
#>

$Project = Join-Path -Path $PSScriptRoot -ChildPath "\..\eng\Tools\DiagConfig\DiagConfig.csproj"
$SlnGen = Join-Path -Path $PSScriptRoot -ChildPath ".\Slngen.ps1"

Write-Output "Building DiagConfig tool"
& dotnet build $Project --nologo --verbosity q

Write-Output "Creating solution file"
& $SlnGen -all -folders -nolaunch -quiet

Write-Output "Restoring packages"
& dotnet restore --nologo --verbosity q

$Artifacts = Join-Path -Path $PSScriptRoot -ChildPath "\..\artifacts" -Resolve
$DiagToolPath = "$Artifacts\bin\DiagConfig\Debug\net8.0\*"
$Diags = (Resolve-Path $PSScriptRoot).Path + "\..\eng\Diags"
# Project which will be used to fetch the analyzer list.
$AsyncStateProjectPath = (Resolve-Path $PSScriptRoot).Path + "\..\test\Libraries\Microsoft.AspNetCore.AsyncState.Tests\Microsoft.AspNetCore.AsyncState.Tests.csproj"

# In this section, we dynamically fetch the list of analyzers we should use by calling a target from one project which will return us the full list. To do so,
# we must capture the msbuild output of the invocation of that target which returns a list of strings (one string for each line of output). Then we join all of these
# lines into a single one, and we use a simple Regex to get the full list of analyzers.
$_outputArray = & dotnet msbuild $AsyncStateProjectPath /t:GetAnalyzersPassedToCompiler /p:TargetFramework=net8.0
$_output = $_outputArray -join "`n"
$analyzers = $_output -match "Analyzers: (.+)$" | ForEach-Object { $matches[1] -split ',' }

Write-Output "Processing analyzer assemblies"

$tempDir = "$PSScriptRoot\Temp"

if (-not (Test-Path -Path $tempDir)) {
    New-Item -Path $tempDir -ItemType directory | Out-Null
}

Push-Location $tempDir

try {

    foreach ( $a in $analyzers )
    {
        Copy-Item -Path $a -Destination $tempDir
    }

    Copy-Item -Path $DiagToolPath -Destination $tempDir

    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.Analyzers.Extra.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.Analyzers.Local.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge StyleCop.Analyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge SonarAnalyzer.CSharp.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.VisualStudio.Threading.Analyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.VisualStudio.Threading.Analyzers.CSharp.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge xunit.analyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.CodeAnalysis.CodeStyle.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.CodeAnalysis.NetAnalyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge ILlink.RoslynAnalyzer.dll
#    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.AspNetCore.App.Analyzers.dll
    & dotnet exec .\DiagConfig.dll $Diags analyzer merge Microsoft.AspNetCore.Components.Analyzers.dll
} finally {
    Pop-Location
    Remove-Item -Path $tempDir -Recurse
}
