<#
.SYNOPSIS
    A script to diff the contents of folders matching a specified pattern between two specified branches.

.DESCRIPTION
    The script uses git to determine the set of files (under folders matching a specified pattern) that are different
    between the specified branches. It can also optionally display the line diffs for these files.

.PARAMETER baseline
    The baseline branch against which the specified target branch is to be compared. (Defaults to 'main' if omitted.)
.PARAMETER target
    The target branch which is to be compared against the specified baseline branch.
.PARAMETER folderPattern
    The pattern that selects the folders that are to be compared. (Defaults to '*.AI.* if omitted.)
.PARAMETER showDiff
    Determines whether or not line diffs should be displayed for the differing files. (Defaults to 'false' if omitted.)

.EXAMPLE
    PS> .\DiffBranches -target "release/9.5" -folderPattern "*.Evaluation.*"
.EXAMPLE
    PS> .\DiffBranches -baseline "release/9.4" -target "release/9.5" -folderPattern "*.Evaluation.*"
.EXAMPLE
    PS> .\DiffBranches -target "release/9.5" -showDiff
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$baseline = "main",
    
    [Parameter(Mandatory=$true)]
    [string]$target,
    
    [Parameter(Mandatory=$false)]
    [string]$folderPattern = "*.AI.*",
    
    [Parameter(Mandatory=$false)]
    [switch]$showDiff
)

function Invoke-GitCommand {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Command,

        [Parameter(Mandatory=$false)]
        [switch]$UseCmd
    )

    if ($UseCmd) {
        $Command = "cmd.exe /c $Command"
    }
        
    Write-Host "Executing $Command" -ForegroundColor Blue
    $result = Invoke-Expression $Command
    return $result
}

# Save the current directory
$originalLocation = Get-Location

try {
    # Get the root directory of the git repository
    $gitRootCommand = "git rev-parse --show-toplevel"
    $repoRoot = Invoke-GitCommand -Command $gitRootCommand
    Write-Host "Repo root is $repoRoot" -ForegroundColor Blue

    # Change to the repository root directory
    Set-Location $repoRoot
    
    # Get all changed files between the two branches
    $gitFilesCommand = "git diff --name-only $baseline..$target"
    $changedFiles = Invoke-GitCommand -Command $gitFilesCommand

    # Filter for files under folders containing the specified pattern
    $matchedFiles = $changedFiles | Where-Object { 
        $path = $_
        $folders = $path -split '/'
        $folders | Where-Object { $_ -like $folderPattern } | Select-Object -First 1
    }

    if ($matchedFiles.Count -eq 0) {
        Write-Host "No changes detected." -ForegroundColor Green
    } else {
        Write-Host "Changes detected in following files:" -ForegroundColor Yellow
        $matchedFiles | ForEach-Object { Write-Host "  $_" }
        
        if ($showDiff) {
            Write-Host "File diffs:" -ForegroundColor Yellow
            
            $gitDiffCommand = "git -C `"$repoRoot`" diff --color $baseline..$target -- $($matchedFiles -join ' ')"
            
            # Use the -UseCmd switch to run the command with cmd.exe (preserves color and paging)
            Invoke-GitCommand -Command $gitDiffCommand -UseCmd
        }
    }
}
finally {
    # Return to the original directory even if an error occurs
    Set-Location $originalLocation
}
