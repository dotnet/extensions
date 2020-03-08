$ErrorActionPreference = 'Stop'
# Update the default TLS support to 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Invoke-Block([scriptblock]$cmd, [string]$WorkingDir = $null) {
    if ($WorkingDir) {
        Push-Location $WorkingDir
    }

    try {

        $cmd | Out-String | Write-Verbose
        & $cmd

        # Need to check both of these cases for errors as they represent different items
        # - $?: did the powershell script block throw an error
        # - $lastexitcode: did a windows command executed by the script block end in error
        if ((-not $?) -or ($lastexitcode -ne 0)) {
            if ($error -ne $null)
            {
                Write-Warning $error[0]
            }
            throw "Command failed to execute: $cmd"
        }
    }
    finally {
        if ($WorkingDir) {
            Pop-Location
        }
    }
}
