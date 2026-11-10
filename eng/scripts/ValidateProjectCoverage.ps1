#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the code coverage policy for each project.
.DESCRIPTION
    This script compares code coverage with thresholds given in "MinCodeCoverage" property in each project.
    The script writes an error for each project that does not comply with the policy.
.PARAMETER CoberturaReportXml
    Path to the XML file to read the code coverage report from in Cobertura format
.PARAMETER PullRequestBase
    Git revision to compare with HEAD when filtering the GitHub comment. Validation remains repository-wide.
.EXAMPLE
    PS> .\ValidatePerProjectCoverage.ps1 -CoberturaReportXml .\Cobertura.xml
#>

param (
    [Parameter(Mandatory = $true, HelpMessage="Path to the XML file to read the code coverage report from")]
    [string]$CoberturaReportXml,
    [string]$PullRequestBase
)

function Write-Header {
    param($message, [bool]$isError);
    $color = if ($isError) { 'Red' } else { 'Green' };
    Write-Host $message -ForegroundColor $color;
    Write-Host ("=" * 80)
 }
function Get-XmlValue { param($X, $Y); return $X.SelectSingleNode($Y).'#text' }

function Format-CoverageReport {
    param([string[]]$ErrorRows, [string[]]$KudosRows)

    $sections = @()
    if ($ErrorRows.Count -gt 0) {
        $sections += @(":bangbang: **Found issues** :bangbang: ", "`r`n", '| Project | Coverage Type |Expected | Actual | ', '| --- | :---: | ---: | ---: |') + $ErrorRows + @("`r`n`r`n")
    }

    if ($KudosRows.Count -gt 0) {
        $sections += @(':tada: **Good job! The coverage increased** :tada:', 'Update `MinCodeCoverage` in the project files.', "`r`n", '| Project | Expected | Actual |', '| --- | ---: | ---: |') + $KudosRows + @("`r`n`r`n")
    }

    return $sections -join "`r`n"
}

Write-Verbose "Reading cobertura report..."
[xml]$CoberturaReport = Get-Content $CoberturaReportXml
if ($null -eq $CoberturaReport.coverage -or 
    $null -eq $CoberturaReport.coverage.packages -or
    $null -eq $CoberturaReport.coverage.packages.package -or 
    0 -eq $CoberturaReport.coverage.packages.package.count)
{
    return
}

$ProjectToMinCoverageMap = @{}
$ProjectPathToAssemblyMap = @{}

Get-ChildItem -Path src -Include '*.*sproj' -Recurse | ForEach-Object {
    $XmlDoc = [xml](Get-Content $_)
    $AssemblyName = Get-XmlValue $XmlDoc "//Project/PropertyGroup/AssemblyName"
    $MinCodeCoverage = Get-XmlValue $XmlDoc "//Project/PropertyGroup/MinCodeCoverage"

    if ([string]::IsNullOrWhiteSpace($AssemblyName)) {
        $AssemblyName = $_.BaseName
    }

    $ProjectPathToAssemblyMap[$_.FullName] = $AssemblyName

    if ([string]::IsNullOrWhiteSpace($MinCodeCoverage)) {
        # Test projects may not legitimely have min code coverage set.
        Write-Warning "$AssemblyName doesn't declare 'MinCodeCoverage' property"
        return
    }

    $ProjectToMinCoverageMap[$AssemblyName] = $MinCodeCoverage
}

$ReportAllProjects = [string]::IsNullOrWhiteSpace($PullRequestBase)
$CommentProjects = @{}
if (!$ReportAllProjects) {
    # PR builds check out a merge commit; its first parent is the target revision used for that build.
    $diff = git diff --name-only --no-renames -z $PullRequestBase HEAD --
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot determine changed files relative to '$PullRequestBase'."
    }

    $changedFiles = (($diff -join "`n") -split "`0") | Where-Object { $_ -ne '' }
    $projects = @(Get-ChildItem -Path src, test -Include '*.*sproj' -Recurse)
    foreach ($file in $changedFiles) {
        if ($file -match '^(src/(Shared|LegacySupport)/|test/Shared/)') {
            $ReportAllProjects = $true
            break
        }

        $fullPath = [System.IO.Path]::GetFullPath($file)
        $owners = @($projects | Where-Object {
            $fullPath.StartsWith($_.DirectoryName + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::Ordinal)
        } | Sort-Object { $_.DirectoryName.Length } -Descending)

        if ($owners.Count -gt 0) {
            # The deepest directory with a matching project file
            $ownerDirectory = $owners[0].DirectoryName
            foreach ($project in $owners | Where-Object { $_.DirectoryName -eq $ownerDirectory }) {
                if ($ProjectPathToAssemblyMap.ContainsKey($project.FullName)) {
                    $CommentProjects[$ProjectPathToAssemblyMap[$project.FullName]] = $true
                }
                else {
                    # This should be a test project (anything not under src)
                    $projectXml = [xml](Get-Content $project.FullName)
                    foreach ($reference in $projectXml.SelectNodes('//ProjectReference[@Include]')) {
                        $referencePath = [System.IO.Path]::GetFullPath(
                            (Join-Path $project.DirectoryName ($reference.Include.Replace('\', [System.IO.Path]::DirectorySeparatorChar))))
                        if ($ProjectPathToAssemblyMap.ContainsKey($referencePath)) {
                            $CommentProjects[$ProjectPathToAssemblyMap[$referencePath]] = $true
                        }
                    }
                }
            }
        }
        elseif ($file -match '^(src/|test/|eng/|scripts/|\.config/dotnet-tools\.json$|build\.(sh|cmd)$|azure-pipelines.*\.yml$|[^/]+\.(props|targets)$|global\.json$|NuGet\.config$)') {
            $ReportAllProjects = $true
            break
        }
    }
}

$esc = [char]27
$Errors = New-Object System.Collections.ArrayList
$Kudos = New-Object System.Collections.ArrayList
$ErrorsMarkdown = @();
$KudosMarkdown = @();
$CommentErrorsMarkdown = @();
$CommentKudosMarkdown = @();
$FatalErrors = 0;
$Warnings = 0;

Write-Verbose "Collecting projects from code coverage report..."
$CoberturaReport.coverage.packages.package | ForEach-Object {
    $Name = $_.name
    $LineCoverage = [math]::Round([double]$_.'line-rate' * 100, 2)
    $BranchCoverage = [math]::Round([double]$_.'branch-rate' * 100, 2)
    $IsFailed = $false
    $IsWarning = $false
    $IncludeInComment = $ReportAllProjects -or $CommentProjects.ContainsKey($Name)

    Write-Verbose "Project $Name with line coverage $LineCoverage and branch coverage $BranchCoverage"

    if ($ProjectToMinCoverageMap.ContainsKey($Name)) {
        if ($ProjectToMinCoverageMap[$Name] -eq 'n/a')
        {
            Write-Host "$Name ...code coverage is not applicable"
            return
        }

        [double]$MinCodeCoverage = $ProjectToMinCoverageMap[$Name]

        # Detect the under-coverage
        if ($MinCodeCoverage -gt $LineCoverage) {
            if ($MinCodeCoverage -eq 100) {
                $ansiEscapeCode = "$esc[1m$esc[0;31m";
                $IsFailed = $true
                $FatalErrors++;
            }
            else {
                $ansiEscapeCode = "$esc[1m$esc[0;33m";
                $IsWarning = $true;
                $Warnings++;
            }

            $row = "| $Name | Line | **$MinCodeCoverage** | $LineCoverage :small_red_triangle_down: |"
            $ErrorsMarkdown += $row
            if ($IncludeInComment) { $CommentErrorsMarkdown += $row }
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name.Replace('Microsoft.Extensions.', 'M.E.').Replace('Microsoft.AspNetCore.', 'M.AC.');
                        "Coverage Type" = "Line";
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$($ansiEscapeCode)$($LineCoverage)$esc[0m"
                    }
                )
            )
        }

        if ($MinCodeCoverage -gt $BranchCoverage) {
            if ($MinCodeCoverage -eq 100) {
                $ansiEscapeCode = "$esc[1m$esc[0;31m";
                $IsFailed = $true
                $FatalErrors++;
            }
            else {
                $ansiEscapeCode = "$esc[1m$esc[0;33m";
                $IsWarning = $true;
                $Warnings++;
            }

            $row = "| $Name | Branch | **$MinCodeCoverage** | $BranchCoverage :small_red_triangle_down: |"
            $ErrorsMarkdown += $row
            if ($IncludeInComment) { $CommentErrorsMarkdown += $row }
            [void]$Errors.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name.Replace('Microsoft.Extensions.', 'M.E.').Replace('Microsoft.AspNetCore.', 'M.AC.');
                        "Coverage Type" = "Branch";
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$($ansiEscapeCode)$($BranchCoverage)$esc[0m"
                    }
                )
            )
        }

        # Detect the over-coverage
        [int]$lowestReported = [math]::Min([math]::Truncate($LineCoverage), [math]::Truncate($BranchCoverage));
        Write-Debug "line: $LineCoverage, branch: $BranchCoverage, min: $lowestReported, threshold: $MinCodeCoverage"
        if ([int]$MinCodeCoverage -lt $lowestReported) {
            $row = "| $Name | $MinCodeCoverage | **$lowestReported** |"
            $KudosMarkdown += $row
            if ($IncludeInComment) { $CommentKudosMarkdown += $row }
            [void]$Kudos.Add(
                (
                    New-Object PSObject -Property @{
                        "Project" = $Name.Replace('Microsoft.Extensions.', 'M.E.').Replace('Microsoft.AspNetCore.', 'M.AC.');
                        "Expected" = $MinCodeCoverage;
                        "Actual" = "$esc[1m$esc[0;32m$($lowestReported)$esc[0m";
                    }
                )
            )
        }

        if    ($IsWarning) { Write-Host "$Name" -NoNewline; Write-Host " ...missed the mark" -ForegroundColor Yellow }
        elseif ($IsFailed) { Write-Host "$Name" -NoNewline; Write-Host " ...failed validation" -ForegroundColor Red }
                      else { Write-Host "$Name" -NoNewline; Write-Host " ...ok" -ForegroundColor Green }
    }
    else {
        Write-Host "$Name ...skipping"
    }
}

if ($Kudos.Count -ne 0) {
    Write-Header -message "`r`nGood job! The coverage increased" -isError $false
    $Kudos | `
        Sort-Object Project | `
        Format-Table "Project", `
                    @{ Name="Expected"; Expression="Expected"; Width=10; Alignment = "Right" }, `
                    @{ Name="Actual"; Expression="Actual"; Width=10; Alignment = "Right" } `
                    -AutoSize -Wrap
    Write-Host "##vso[task.logissue type=warning;]Good job! The coverage increased, please update your projects"
}

if ($Errors.Count -ne 0) {
    Write-Header -message "`r`n[!!] Found $($Errors.Count) issues!" -isError ($Errors.Count -ne 0)
    $Errors | `
        Sort-Object Project, 'Coverage Type' | `
        Format-Table "Project", `
                    @{ Name="Expected"; Expression="Expected"; Width=10; Alignment = "Right" }, `
                    @{ Name="Actual"; Expression="Actual"; Width=10; Alignment = "Right" }, `
                    @{ Name="Coverage Type"; Expression="Coverage Type"; Width=10; Alignment = "Center" } `
                    -AutoSize -Wrap
}

# Write out markdown for publishing back to AzDO
Format-CoverageReport -ErrorRows $ErrorsMarkdown -KudosRows $KudosMarkdown | Out-File coverage-report.md -Encoding ascii

# Set the AzDO variable used by GitHubComment@0 task
[string]$markdown = Format-CoverageReport -ErrorRows $CommentErrorsMarkdown -KudosRows $CommentKudosMarkdown
if (![string]::IsNullOrWhiteSpace($markdown)) {
    # Add link back to the Code Coverage board
    $link = "$($env:SYSTEM_COLLECTIONURI)$env:SYSTEM_TEAMPROJECT/_build/results?buildId=$env:BUILD_BUILDID&view=codecoverage-tab"
    $markdown = "$markdown`n`nFull code coverage report: $link"

    $gitHubCommentVar = '##vso[task.setvariable variable=GITHUB_COMMENT]' + $markdown.Replace("`r`n","`n").Replace("`n","%0D%0A")
    Write-Host $gitHubCommentVar
}

if ($FatalErrors -gt 0)
{
    Write-Host "`r`nBreaking issues detected."
    exit -1;
}

if ($Warnings -gt 0)
{
    Write-Host "`r`nNon-breaking issues detected."
}

if ($FatalErrors -eq 0)
{
    Write-Host "`r`nAll good, no issues detected."
    exit 0;
}

