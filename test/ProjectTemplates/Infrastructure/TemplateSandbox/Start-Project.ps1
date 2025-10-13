[CmdletBinding(PositionalBinding=$false)]
Param(
  [string]$ProjectPath
)

try {
    # Check if there's a solution file in the project folder
    $solutionFile = Get-ChildItem -Path $ProjectPath -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($solutionFile) {
        # Look for a folder ending with .AppHost
        $appHostFolder = Get-ChildItem -Path $ProjectPath -Directory | Where-Object { $_.Name -like "*.AppHost" } | Select-Object -First 1

        if ($appHostFolder) {
            # Find the .csproj file in the .AppHost folder
            $appHostProject = Get-ChildItem -Path $appHostFolder.FullName -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1

            if ($appHostProject) {
                $ProjectPath = $appHostProject.FullName
                Write-Output "Found solution file. Using AppHost project: $ProjectPath"
            }
        }
    }
    else {
        # No solution file, assume ProjectPath points to a project folder
        $projectFile = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($projectFile) {
            $ProjectPath = $projectFile.FullName
        }
    }

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
