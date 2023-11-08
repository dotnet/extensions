#!/usr/bin/env pwsh

<#
.SYNOPSIS
Creates a solution file that contains all the packages referencing specific keywords.
.DESCRIPTION
This script will generate a solution file with all the packages that reference a project matching given keywords.
.EXAMPLE
    PS> .\SlngenReferencing.ps1 AsyncState
#>

param(
    [Parameter(Mandatory = $true, HelpMessage="Keywords to search for references")]
    [string[]]$Keywords
)

function Get-ReferencingPackages([string]$repoRoot, [string]$directory, [System.Collections.Generic.HashSet[string]]$projectNames, [string]$lookingFor) {
    Push-Location $repoRoot

    try {
       Get-ChildItem -Path $directory -Include "*.csproj" -Exclude "Project.Title.csproj" -Recurse | ForEach-Object {
            $projectName = $_.Directory.BaseName;

            (Select-Xml -Path $_.FullName -XPath "/Project/ItemGroup/ProjectReference/@Include").Node.Value | Foreach-Object {

                if ($_ -like "*$lookingFor*") {
                    Write-Output "Found reference to $lookingFor in $projectName." -ForegroundColor Green
                    $projectNames.Add($projectName)
                    return
                }
            }
        } | Out-Null
    }
    finally {
        Pop-Location
    }
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\')
$SlnGenScriptPath = Resolve-Path (Join-Path $RepoRoot '.\scripts\SlnGen.ps1')

[System.Collections.Generic.HashSet[string]]$ProjectNames = @();

$Keywords | ForEach-Object {
    Get-ReferencingPackages $RepoRoot "src/Extensions" $ProjectNames $_
    Get-ReferencingPackages $RepoRoot "src/Service" $ProjectNames $_
}

$NamesString = $ProjectNames -join ","
$NamesStringSpace = $ProjectNames -join ", "
if ($NamesString -eq "") {

    $NamesString = $Keywords -join ",";
    Write-Host "No references found for: ""$NamesString"". Going to build solution file just for those keywords..." -ForegroundColor Yellow
}

Write-Host "Caching is referenced in the following projects [ $NamesStringSpace ]"
powershell $SlnGenScriptPath -Keywords $NamesString -NoLaunch -OutSln "$RepoRoot\SDK-Referencing.sln"