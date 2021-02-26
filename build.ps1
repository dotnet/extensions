# Check dotnet-format is installed or not
$dotnetFormat = Get-Command dotnet-format -ErrorAction Ignore -CommandType Application

if ($dotnetFormat)
{
    Write-Host -f Magenta "dotnet format tool is already installed."
}
else
{
    Write-Host -f Magenta "Installing dotnet-format tool.."
    & dotnet tool install -g dotnet-format
}

# We need to change default git hooks directory as .git folder is not tracked. And by default hooks are stored in .git/hooks folder.
# So we are setting git hooks default directory to .githooks, so that we can track and version the git hooks.
& git config core.hooksPath .githooks

& $PSScriptRoot\eng\common\Build.ps1 -restore -build -pack $args
function ExitWithExitCode([int] $exitCode) {
    if ($ci -and $prepareMachine) {
      Stop-Processes
    }
    exit $exitCode
}

ExitWithExitCode $LASTEXITCODE
