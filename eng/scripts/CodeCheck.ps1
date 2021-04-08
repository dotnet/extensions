#requires -version 5
<#
.SYNOPSIS
This script runs a quick check for common errors, such as checking that Visual Studio solutions are up to date or that generated code has been committed to source.
#>
param(
    [switch]$ci
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$repoRoot = Resolve-Path "$PSScriptRoot/../.."

[string[]] $errors = @()

function LogError {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$message,
        [string]$FilePath
    )
    if ($env:TF_BUILD) {
        $prefix = "##vso[task.logissue type=error"
        if ($FilePath) {
            $prefix = "${prefix};sourcepath=$FilePath"
        }
        Write-Host "${prefix}]${message}"
    }
    Write-Host -f Red "error: $message"
    $script:errors += $message
}

try {
    if ($ci) {
        $env:DOTNET_ROOT = "$repoRoot\.dotnet"
        $env:PATH = "$env:DOTNET_ROOT;$env:PATH"

        & $PSScriptRoot\..\common\build.ps1 -ci -prepareMachine -build:$false -restore:$false
    }

    Write-Host "Checking that Versions.props and Version.Details.xml match"
    [xml] $versionProps = Get-Content "$repoRoot/eng/Versions.props"
    [System.Xml.XmlNamespaceManager] $nsMgr = New-Object -TypeName System.Xml.XmlNamespaceManager($versionProps.NameTable)
    $nsMgr.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003");

    [xml] $versionDetails = Get-Content "$repoRoot/eng/Version.Details.xml"

    $versionVars = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($vars in $versionProps.SelectNodes("//ns:PropertyGroup[`@Label=`"Automated`"]/*", $nsMgr)) {
        $versionVars.Add($vars.Name) | Out-Null
    }

    foreach ($dep in $versionDetails.SelectNodes('//Dependency')) {
        if ($dep.Name -eq 'Microsoft.DotNet.Arcade.Sdk') {
            # Special case - this version is in global.json, not Version.props
            continue
        }
        Write-Verbose "Found $dep"
        $varName = $dep.Name -replace '\.',''
        $varName = $varName -replace '\-',''
        $varName = "${varName}PackageVersion"
        $versionVar = $versionProps.SelectSingleNode("//ns:PropertyGroup[`@Label=`"Automated`"]/ns:$varName", $nsMgr)
        if (-not $versionVar) {
            LogError "Missing version variable '$varName' in the 'Automated' property group in $repoRoot/eng/Versions.props"
            continue
        }

        $versionVars.Remove($varName) | Out-Null

        $expectedVersion = $dep.Version
        $actualVersion = $versionVar.InnerText

        if ($expectedVersion -ne $actualVersion) {
            LogError `
                "Version variable '$varName' does not match the value in Version.Details.xml. Expected '$expectedVersion', actual '$actualVersion'" `
                -filepath "$repoRoot\eng\Versions.props"
        }
    }

    foreach ($unexpectedVar in $versionVars) {
        LogError `
            "Version variable '$unexpectedVar' does not have a matching entry in Version.Details.xml. See https://github.com/dotnet/aspnetcore/blob/main/docs/ReferenceResolution.md for instructions on how to add a new dependency." `
            -filepath "$repoRoot\eng\Versions.props"
    }

    Write-Host "Checking that solutions are up to date"

    Get-ChildItem "$repoRoot/*.sln" -Recurse `
        | % {
        Write-Host "  Checking $(Split-Path -Leaf $_)"
        $slnDir = Split-Path -Parent $_
        $sln = $_
        & dotnet sln $_ list `
            | ? { $_ -like '*proj' } `
            | % {
                $proj = Join-Path $slnDir $_
                if (-not (Test-Path $proj)) {
                    LogError "Missing project. Solution references a project which does not exist: $proj. [$sln] "
                }
            }
        }
}
finally {
    Write-Host ""
    Write-Host "Summary:"
    Write-Host ""
    Write-Host "   $($errors.Length) error(s)"
    Write-Host ""

    foreach ($err in $errors) {
        Write-Host -f Red "error : $err"
    }

    if ($errors) {
        exit 1
    }
}
