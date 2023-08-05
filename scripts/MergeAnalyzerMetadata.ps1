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

# TODO: needs to be auto-generated. This is a dump of the Analyzers items when compiling test/libraries/microsoft.aspnetcore.asyncstate
$analyzers = @(
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\analyzers\Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll'
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\analyzers\Microsoft.CodeAnalysis.NetAnalyzers.dll'
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\codestyle\cs\Microsoft.CodeAnalysis.CodeStyle.dll'
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\codestyle\cs\Microsoft.CodeAnalysis.CodeStyle.Fixes.dll'
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\codestyle\cs\Microsoft.CodeAnalysis.CSharp.CodeStyle.dll'
    'C:\src\dotnet\extensions\.dotnet\sdk\8.0.100-preview.7.23360.1\Sdks\Microsoft.NET.Sdk\targets\..\codestyle\cs\Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.extensions.logging.abstractions\8.0.0-rc.1.23402.13\analyzers\dotnet\roslyn4.4\cs\Microsoft.Extensions.Logging.Generators.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.extensions.options\8.0.0-rc.1.23402.13\analyzers\dotnet\roslyn4.4\cs\Microsoft.Extensions.Options.SourceGeneration.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.net.illink.tasks\8.0.0-preview.7.23359.1\analyzers\dotnet\cs\ILLink.CodeFixProvider.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.net.illink.tasks\8.0.0-preview.7.23359.1\analyzers\dotnet\cs\ILLink.RoslynAnalyzer.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.visualstudio.threading.analyzers\17.5.22\analyzers\cs\Microsoft.VisualStudio.Threading.Analyzers.CSharp.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.visualstudio.threading.analyzers\17.5.22\analyzers\cs\Microsoft.VisualStudio.Threading.Analyzers.CodeFixes.dll'
    'C:\Users\mataille\.nuget\packages\microsoft.visualstudio.threading.analyzers\17.5.22\analyzers\cs\Microsoft.VisualStudio.Threading.Analyzers.dll'
    'C:\Users\mataille\.nuget\packages\sonaranalyzer.csharp\8.52.0.60960\analyzers\Google.Protobuf.dll'
    'C:\Users\mataille\.nuget\packages\sonaranalyzer.csharp\8.52.0.60960\analyzers\SonarAnalyzer.CFG.dll'
    'C:\Users\mataille\.nuget\packages\sonaranalyzer.csharp\8.52.0.60960\analyzers\SonarAnalyzer.CSharp.dll'
    'C:\Users\mataille\.nuget\packages\sonaranalyzer.csharp\8.52.0.60960\analyzers\SonarAnalyzer.dll'
    'C:\Users\mataille\.nuget\packages\stylecop.analyzers.unstable\1.2.0.507\analyzers\dotnet\cs\StyleCop.Analyzers.CodeFixes.dll'
    'C:\Users\mataille\.nuget\packages\stylecop.analyzers.unstable\1.2.0.507\analyzers\dotnet\cs\StyleCop.Analyzers.dll'
    'C:\src\dotnet\extensions\artifacts\bin\Microsoft.Analyzers.Extra\Debug\netstandard2.0\Microsoft.Analyzers.Extra.dll'
    'C:\src\dotnet\extensions\artifacts\bin\Microsoft.Analyzers.Local\Debug\netstandard2.0\Microsoft.Analyzers.Local.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/Microsoft.Interop.ComInterfaceGenerator.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/Microsoft.Interop.JavaScript.JSImportGenerator.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/Microsoft.Interop.LibraryImportGenerator.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/Microsoft.Interop.SourceGeneration.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/System.Text.Json.SourceGeneration.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.NETCore.App.Ref\8.0.0-preview.7.23359.1\analyzers/dotnet/cs/System.Text.RegularExpressions.Generator.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.AspNetCore.App.Ref\8.0.0-preview.7.23359.2\analyzers/dotnet/cs/Microsoft.AspNetCore.App.Analyzers.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.AspNetCore.App.Ref\8.0.0-preview.7.23359.2\analyzers/dotnet/cs/Microsoft.AspNetCore.App.CodeFixes.dll'
    'C:\src\dotnet\extensions\.dotnet\packs\Microsoft.AspNetCore.App.Ref\8.0.0-preview.7.23359.2\analyzers/dotnet/cs/Microsoft.AspNetCore.Components.Analyzers.dll'
    'C:\Users\mataille\.nuget\packages\xunit.analyzers\1.1.0\analyzers\dotnet\cs\xunit.analyzers.dll'
    'C:\Users\mataille\.nuget\packages\xunit.analyzers\1.1.0\analyzers\dotnet\cs\xunit.analyzers.fixes.dll'
)

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

    & .\DiagConfig.exe $Diags analyzer merge Microsoft.Analyzers.Extra.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.Analyzers.Local.dll
    & .\DiagConfig.exe $Diags analyzer merge StyleCop.Analyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge SonarAnalyzer.CSharp.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.VisualStudio.Threading.Analyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.VisualStudio.Threading.Analyzers.CSharp.dll
    & .\DiagConfig.exe $Diags analyzer merge xunit.analyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CodeStyle.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.NetAnalyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge ILlink.RoslynAnalyzer.dll
#    & .\DiagConfig.exe $Diags analyzer merge Microsoft.AspNetCore.App.Analyzers.dll
    & .\DiagConfig.exe $Diags analyzer merge Microsoft.AspNetCore.Components.Analyzers.dll
} finally {
    Pop-Location
    Remove-Item -Path $tempDir -Recurse
}
