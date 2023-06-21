#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the code coverage policy for each project.
.DESCRIPTION
    This script compares code coverage with thresholds given in "MinCodeCoverage" property in each project.
    The script writes an error for each project that does not comply with the policy.
.PARAMETER CoberturaReportXml
    Path to the XML file to read the code coverage report from in Cobertura format
.PARAMETER OnlyForProjects
    Optional set of projects to check coverage for only.
.EXAMPLE
    PS> .\ValidatePerProjectCoverage.ps1 -CoberturaReportXml .\Cobertura.xml -OnlyForProjects '.\src\Project1\Project1.csproj','.\src\Project2\Project2.csproj'
#>

param (
    [Parameter(Mandatory = $true, HelpMessage="Path to the XML file to read the code coverage report from")]
    [string]$CoberturaReportXml,
    [Parameter(Mandatory = $false, HelpMessage="Optional set of projects to check coverage for only.")]
    [string[]]$OnlyForProjects
)

function Write-Header { param($m); Write-Output $NL$m, ("=" * 80), $NL }
function Get-XmlValue { param($X, $Y); return $X.SelectSingleNode($Y).'#text' }

Write-Verbose "Reading cobertura report..."
[xml]$CoberturaReport = Get-Content $CoberturaReportXml

$ProjectFileList = New-Object System.Collections.ArrayList

if ($OnlyForProjects -and $OnlyForProjects.Count -gt 0) {
    $ProjectFileList.AddRange($OnlyForProjects)
}
else {
    $ProjectFileList.AddRange((Get-ChildItem -Path src `
        -Include '*.*sproj' `
        -Exclude ('Project.Title.csproj', '*ProjectTemplate.csproj', 'Templates.csproj') `
        -Recurse))
}

$ProjectToMinCoverageMap = @{}

$ProjectFileList | ForEach-Object {
    $XmlDoc = [xml](Get-Content $_)
    $AssemblyName = Get-XmlValue $XmlDoc "//Project/PropertyGroup/AssemblyName"
    $MinCodeCoverage = Get-XmlValue $XmlDoc "//Project/PropertyGroup/MinCodeCoverage"
    $TempMinCodeCoverage = Get-XmlValue $XmlDoc "//Project/PropertyGroup/TempMinCodeCoverage"

    if ([string]::IsNullOrWhiteSpace($AssemblyName)) {
        # Assembly name is empty for template projects and for packages projects.
        return
    }

    if ([string]::IsNullOrWhiteSpace($MinCodeCoverage)) {
        # Test projects may not legitimely have min code coverage set.
        return
    }

    # Some projects currently fail code coverage checks. Allow to temporarily override the requirements
    # See https://github.com/dotnet/r9/issues/75
    if (![string]::IsNullOrWhiteSpace($TempMinCodeCoverage)) {
        $MinCodeCoverage = $TempMinCodeCoverage
    }

    $ProjectToMinCoverageMap[$AssemblyName] = $MinCodeCoverage
}

$Errors = New-Object System.Collections.ArrayList

if ($null -eq $CoberturaReport.coverage -or $null -eq $CoberturaReport.coverage.packages)
{
    return
}

if ($null -eq $CoberturaReport.coverage.packages.package -or 0 -eq $CoberturaReport.coverage.packages.package.count)
{
    return
}

Write-Verbose "Collecting projects from code coverage report..."
$CoberturaReport.coverage.packages.package | ForEach-Object {
    $Name = $_.name
    $LineCoverage = [math]::Round([double]$_.'line-rate' * 100, 2)
    $BranchCoverage = [math]::Round([double]$_.'branch-rate' * 100, 2)
    $IsFailed = $false

    Write-Verbose "Project $Name with line coverage $LineCoverage and branch coverage $BranchCoverage"

    if ($ProjectToMinCoverageMap.ContainsKey($Name))
    {
        if ($ProjectToMinCoverageMap[$Name] -eq 'n/a')
        {
            Write-Output "$Name ...code coverage is not applicable"
            return
        }

        [double]$MinCodeCoverage = $ProjectToMinCoverageMap[$Name]

        if ($MinCodeCoverage -gt $LineCoverage)
        {
            $IsFailed = $true
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project"=$Name;"Coverage Type"="Line";
                        "Actual Coverage"=$LineCoverage;"Expected Coverage"=$MinCodeCoverage
                    }
                )
            )
        }

        if ($MinCodeCoverage -gt $BranchCoverage)
        {
            $IsFailed = $true
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project"=$Name;"Coverage Type"="Branch";
                        "Actual Coverage"=$BranchCoverage;"Expected Coverage"=$MinCodeCoverage
                    }
                )
            )
        }

        if ($IsFailed) { Write-Output "$Name ...failed validation" }
                  else { Write-Output "$Name ...ok" }
    }
    else {
        Write-Output "$Name ...skipping"
    }
}

if ($Errors.Count -ge 1)
{
    Write-Output ""
    Write-Header "[!!] Found $($Errors.Count) issues!"
    $Errors | Sort-Object Project, 'Coverage Type' | Format-Table "Project", "Coverage Type", "Actual Coverage", "Expected Coverage" -AutoSize -wrap

    throw "Validation failed (see the report above)!"
}

Write-Output ""
Write-Output "All good, no issues found."