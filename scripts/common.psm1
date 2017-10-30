function Invoke-Block([scriptblock]$cmd) {
    & $cmd

    # Need to check both of these cases for errors as they represent different items
    # - $?: did the powershell script block throw an error
    # - $lastexitcode: did a windows command executed by the script block end in error
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        throw "Command failed to execute: $cmd"
    }
}

function Get-DotNet {
    if ($env:DOTNET_TOOL_DIR) {
        return Join-Path $env:DOTNET_TOOL_DIR dotnet.exe
    } 

    $command = Get-Command dotnet -ErrorAction Ignore
    if (-not $command) {
        throw 'Could not find dotnet.exe on the PATH'
    }
    return $command.Path
}
