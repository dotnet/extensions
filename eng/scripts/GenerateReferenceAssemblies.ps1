#!/usr/bin/env pwsh -c
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
if (-not (Test-Path Variable:\IsCoreClr)) {
    $IsWindows = $true
}
$ext = if ($IsWindows) { '.ps1' } else { '.sh' }
& "$repoRoot/eng/common/msbuild${ext}" "$repoRoot/eng/repo.targets" /t:GenerateReferenceSources /bl
