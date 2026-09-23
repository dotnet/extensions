$ErrorActionPreference = "Stop"

$TestRoot = Join-Path ([System.IO.Path]::GetTempPath()) "azure-devops-report-build-$([guid]::NewGuid())"
$OriginalPath = $env:PATH

try {
    $TestScriptRoot = Join-Path $TestRoot "azure-devops-report"
    $TaskPath = Join-Path $TestScriptRoot "tasks/PublishAIEvaluationReport"
    $StubPath = Join-Path $TestRoot "bin"
    $OutputPath = Join-Path $TestRoot "output"
    $NpmCallsPath = Join-Path $TestRoot "npm-calls.txt"
    $NpxCallsPath = Join-Path $TestRoot "npx-calls.txt"

    New-Item -ItemType Directory -Path $TaskPath, $StubPath, $OutputPath | Out-Null
    Copy-Item (Join-Path $PSScriptRoot "build.ps1") $TestScriptRoot

    $StaleDirectories = @(
        (Join-Path $TaskPath "node_modules"),
        (Join-Path $TaskPath "dist"),
        (Join-Path $TestScriptRoot "node_modules"),
        (Join-Path $TestScriptRoot "dist")
    )

    foreach ($Directory in $StaleDirectories) {
        New-Item -ItemType Directory -Path $Directory | Out-Null
        Set-Content -Path (Join-Path $Directory "stale.txt") -Value "stale"
    }

    if ($IsWindows) {
        Set-Content -Path (Join-Path $StubPath "npm.cmd") -Value "@echo off`r`necho npm>>`"%NPM_CALLS%`"`r`nexit /b 17`r`n" -NoNewline
        Set-Content -Path (Join-Path $StubPath "npx.cmd") -Value "@echo off`r`necho npx>>`"%NPX_CALLS%`"`r`nexit /b 0`r`n" -NoNewline
    }
    else {
        $NpmStub = Join-Path $StubPath "npm"
        $NpxStub = Join-Path $StubPath "npx"
        Set-Content -Path $NpmStub -Value "#!/bin/sh`necho npm >> `"`$NPM_CALLS`"`nexit 17`n" -NoNewline
        Set-Content -Path $NpxStub -Value "#!/bin/sh`necho npx >> `"`$NPX_CALLS`"`nexit 0`n" -NoNewline
        chmod +x $NpmStub $NpxStub
    }

    $env:NPM_CALLS = $NpmCallsPath
    $env:NPX_CALLS = $NpxCallsPath
    $env:PATH = "$StubPath$([System.IO.Path]::PathSeparator)$OriginalPath"

    $BuildOutput = & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $TestScriptRoot "build.ps1") -OutputPath $OutputPath -Version "1.0.0" 2>&1
    $BuildExitCode = $LASTEXITCODE
    $BuildOutput | Write-Host

    if ($BuildExitCode -eq 0) {
        throw "The build succeeded after npm exited with a nonzero code."
    }

    if ("$BuildOutput" -notmatch "exit code 17") {
        throw "The build did not report the npm exit code."
    }

    if ((Get-Content $NpmCallsPath).Count -ne 1) {
        throw "The build did not stop immediately after the failed npm command."
    }

    if (Test-Path $NpxCallsPath) {
        throw "The build invoked npx after npm failed."
    }

    foreach ($Directory in $StaleDirectories) {
        if (Test-Path $Directory) {
            throw "The stale build directory '$Directory' was reused."
        }
    }

    if (Get-ChildItem -Path $OutputPath -Filter "*.vsix") {
        throw "The build produced a VSIX after npm failed."
    }
}
finally {
    $env:PATH = $OriginalPath
    Remove-Item Env:NPM_CALLS -ErrorAction SilentlyContinue
    Remove-Item Env:NPX_CALLS -ErrorAction SilentlyContinue
    Remove-Item -Path $TestRoot -Recurse -Force -ErrorAction SilentlyContinue
}
