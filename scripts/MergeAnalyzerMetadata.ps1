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

# $DotnetVersion = & dotnet --version
$DotNetVersion = "8.0.100-preview.5.23262.6"

$Artifacts = Join-Path -Path $PSScriptRoot -ChildPath "\..\artifacts" -Resolve
$DiagToolPath = "$Artifacts\bin\DiagConfig\Debug\net8.0\*"
$Command = "$Artifacts\bin\DiagConfig\Debug\net8.0\DiagConfig.exe"
$Diags = (Resolve-Path $PSScriptRoot).Path + "\..\eng\Diags"
#$DotnetPath = Split-Path -Path (get-command dotnet.exe).Path -Parent
$DotnetPath = "C:\Program Files\dotnet"
$RoslynPath = "$DotnetPath\sdk\$DotnetVersion\Roslyn\bincore"
$CodeStylePath = "$DotnetPath\sdk\$DotnetVersion\Sdks\Microsoft.NET.Sdk\codestyle\cs"
$NetAnalyzersPath = "$DotnetPath\sdk\$DotnetVersion\Sdks\Microsoft.NET.Sdk\analyzers"
$PackagePath = Join-Path -Path $Home -ChildPath ".nuget/packages" -Resolve
$AspNetCorePath = Join-Path -Path $DotNetPath -ChildPath packs\Microsoft.AspNetCore.App.Ref\7.0.4\analyzers\dotnet\cs

Write-Output "Processing analyzer assemblies"

& $Command $Diags analyzer merge $Artifacts\bin\Microsoft.Extensions.ExtraAnalyzers.Roslyn4.0\Debug\netstandard2.0\Microsoft.Extensions.ExtraAnalyzers.Roslyn4.0.dll
& $Command $Diags analyzer merge $Artifacts\bin\Microsoft.Extensions.LocalAnalyzers\Debug\netstandard2.0\Microsoft.Extensions.LocalAnalyzers.dll
& $Command $Diags analyzer merge $PackagePath\stylecop.analyzers.unstable\1.2.0.435\analyzers\dotnet\cs\StyleCop.Analyzers.dll
& $Command $Diags analyzer merge $PackagePath\sonaranalyzer.csharp\8.52.0.60960\analyzers\SonarAnalyzer.CSharp.dll
& $Command $Diags analyzer merge $PackagePath\microsoft.visualstudio.threading.analyzers\17.5.22\analyzers\cs\Microsoft.VisualStudio.Threading.Analyzers.dll
& $Command $Diags analyzer merge $PackagePath\microsoft.visualstudio.threading.analyzers\17.5.22\analyzers\cs\Microsoft.VisualStudio.Threading.Analyzers.CSharp.dll
& $Command $Diags analyzer merge $PackagePath\xunit.analyzers\1.0.0\analyzers\dotnet\cs\xunit.analyzers.dll

# voodoo for Microsoft.CodeAnalysis.* and Microsoft.AspNetCore.*

$tempDir = "$PSScriptRoot\Temp"

if (-not (Test-Path -Path $tempDir)) {
    New-Item -Path $tempDir -ItemType directory | Out-Null
}

Push-Location $tempDir

try {
    Copy-Item -Path (Join-Path -Path $RoslynPath -ChildPath Microsoft.CodeAnalysis.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $RoslynPath -ChildPath Microsoft.CodeAnalysis.CSharp.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $CodeStylePath -ChildPath Microsoft.CodeAnalysis.CodeStyle.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $CodeStylePath -ChildPath Microsoft.CodeAnalysis.CSharp.CodeStyle.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $NetAnalyzersPath -ChildPath Microsoft.CodeAnalysis.NetAnalyzers.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $NetAnalyzersPath -ChildPath Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $NetAnalyzersPath -ChildPath ILlink.RoslynAnalyzer.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $AspNetCorePath -ChildPath Microsoft.AspNetCore.App.Analyzers.dll -Resolve) -Destination $tempDir
    Copy-Item -Path (Join-Path -Path $AspNetCorePath -ChildPath Microsoft.AspNetCore.Components.Analyzers.dll -Resolve) -Destination $tempDir
    Copy-Item -Path $DiagToolPath -Destination $tempDir

    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CodeStyle.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.NetAnalyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge ILlink.RoslynAnalyzer.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.AspNetCore.App.Analyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.AspNetCore.Components.Analyzers.dll
} finally {
    Pop-Location
    Remove-Item -Path $tempDir -Recurse
}
