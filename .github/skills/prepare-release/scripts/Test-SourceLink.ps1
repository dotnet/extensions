<#
.SYNOPSIS
  Validates Source Link + symbol-server availability for published NuGet packages,
  the same two things nuget.info ("Valid with Symbol Server") checks.

.DESCRIPTION
  For each .nupkg: extracts a lib DLL, pulls the matching PDB from the NuGet symbol
  server via dotnet-symbol, then runs `sourcelink test` on the PDB.

  Results:
    valid                 Source Link resolved and symbols were on the server
    sourcelink-FAILED     Symbols found, but Source Link did not validate
    symbols-not-indexed   PDB not yet on the symbol server (still "Validating..." on nuget.info)
    no-lib-dll            Package has no lib/**/*.dll (template/tooling package)

  Requires: dotnet tool install -g sourcelink ; dotnet tool install -g dotnet-symbol

.EXAMPLE
  ./Test-SourceLink.ps1 -PackageDir C:\path\to\published\packages
#>
param(
  [string]$PackageDir = '.',
  [string]$SymbolServer = 'https://symbols.nuget.org/download/symbols/',
  [int]$Max = 0   # 0 = all packages
)

$ErrorActionPreference = 'Continue'
$env:PATH += ';' + (Join-Path $env:USERPROFILE '.dotnet\tools')

$tmp = Join-Path $env:TEMP ('srclink-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null

$pkgs = Get-ChildItem -Path $PackageDir -Filter '*.nupkg'
if ($Max -gt 0) { $pkgs = $pkgs | Select-Object -First $Max }

$results = foreach ($pkg in $pkgs) {
    $ed = Join-Path $tmp ([IO.Path]::GetFileNameWithoutExtension($pkg.Name))
    Expand-Archive -Path $pkg.FullName -DestinationPath $ed -Force

    $dll = Get-ChildItem -Path (Join-Path $ed 'lib') -Filter '*.dll' -Recurse -ErrorAction SilentlyContinue |
           Sort-Object { if ($_.Directory.Name -eq 'net8.0') { 0 } else { 1 } } | Select-Object -First 1
    if (-not $dll) { [pscustomobject]@{ Package = $pkg.Name; Result = 'no-lib-dll' }; continue }

    $sd = Join-Path $ed 'sym'; New-Item -ItemType Directory -Path $sd | Out-Null
    Copy-Item $dll.FullName $sd
    dotnet-symbol --symbols --server-path $SymbolServer --output $sd (Join-Path $sd $dll.Name) 2>&1 | Out-Null
    $pdb = Get-ChildItem -Path $sd -Filter '*.pdb' | Select-Object -First 1
    if (-not $pdb) { [pscustomobject]@{ Package = $pkg.Name; Result = 'symbols-not-indexed' }; continue }

    sourcelink test $pdb.FullName 2>&1 | Out-Null
    $res = if ($LASTEXITCODE -eq 0) { 'valid' } else { 'sourcelink-FAILED' }
    [pscustomobject]@{ Package = $pkg.Name; Result = $res }
}

$results | Sort-Object Result, Package | Format-Table -AutoSize
Write-Output ''
$results | Group-Object Result | ForEach-Object { "{0,-22} {1}" -f $_.Name, $_.Count }
Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
