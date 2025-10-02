[CmdletBinding(PositionalBinding=$false)]
Param(
  [string]$ProjectPath
)

try {
    $ProjectName = Split-Path -Path (Split-Path -Path $ProjectPath -Parent) -Leaf
    $OutputDir = "$PSScriptRoot\output\ai\"
    $StdoutPath = "$($OutputDir)$($ProjectName)_stdout.txt"
    $StderrPath = "$($OutputDir)$($ProjectName)_stderr.txt"
    
    # Create output directory if it doesn't exist
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    
    . $PSScriptRoot\activate.ps1
    $proc = Start-Process dotnet `
        -ArgumentList "run --project $ProjectPath" `
        -RedirectStandardOutput $StdoutPath `
        -RedirectStandardError $StderrPath `
        -WindowStyle Hidden `
        -PassThru
    Write-Output "Project running. Process ID: $($proc.Id)"
    Write-Output "Stdout path: $($StdoutPath)"
    Write-Output "Stderr path: $($StderrPath)"
} finally {
    # Always cleanup the environment
    if (Get-Command deactivate -ErrorAction SilentlyContinue) {
        deactivate
    }
}
