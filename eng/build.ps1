[CmdletBinding(PositionalBinding=$false, DefaultParameterSetName = 'CommandLine')]
Param(
  [Parameter(ParameterSetName='CommandLine')]
  [string][Alias('c')]$configuration = "Debug",
  [Parameter(ParameterSetName='CommandLine')]
  [string]$platform = $null,
  [Parameter(ParameterSetName='CommandLine')]
  [string] $projects,
  [Parameter(ParameterSetName='CommandLine')]
  [string][Alias('v')]$verbosity = "minimal",
  [Parameter(ParameterSetName='CommandLine')]
  [string] $msbuildEngine = $null,
  [Parameter(ParameterSetName='CommandLine')]
  [boolean] $warnAsError = $false,        # NOTE: inverted the Arcade's default
  [Parameter(ParameterSetName='CommandLine')]
  [boolean] $nodeReuse = $true,
  [Parameter(ParameterSetName='CommandLine')]
  [Parameter(ParameterSetName='VisualStudio')]
  [switch][Alias('r')]$restore,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $deployDeps,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('b')]$build,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $rebuild,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $deploy,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('t')]$test,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $integrationTest,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $performanceTest,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $sign,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $pack,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $publish,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $clean,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('bl')]$binaryLog,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('nobl')]$excludeCIBinarylog,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $ci,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $prepareMachine,
  [Parameter(ParameterSetName='CommandLine')]
  [string] $runtimeSourceFeed = '',
  [Parameter(ParameterSetName='CommandLine')]
  [string] $runtimeSourceFeedKey = '',
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $excludePrereleaseVS,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $nativeToolsOnMachine,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $help,

  # Run tests with code coverage
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $testCoverage,

  [Parameter(ParameterSetName='CommandLine')]
  [Parameter(ParameterSetName='VisualStudio')]
  [string[]] $onlyTfms = $null,

  [Parameter(ParameterSetName='VisualStudio')]
  [string[]] $vs = $null,
  [Parameter(ParameterSetName='VisualStudio')]
  [switch] $noLaunch = $false,

  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Print-Usage() {
  Write-Host "Custom settings:"
  Write-Host "  -testCoverage           Run unit tests and capture code coverage information."
  Write-Host "  -vs <value>             Comma delimited list of keywords to filter the projects in the solution."
  Write-Host "                          Pass '*' to generate a solution with all projects."
  Write-Host "  -noLaunch               Don't open the generated solution in Visual Studio (only if -vs specified)"
  Write-Host "  -onlyTfms <value>       Semi-colon delimited list of TFMs to build (e.g. 'net8.0;net6.0')"
  Write-Host ""
}

if ($help) {
  Get-Help $PSCommandPath

  Print-Usage;
  . $PSScriptRoot/common/build.ps1 -help
  exit 0
}

if ($onlyTfms.Count -gt 0) {
  $onlyTfms -join ';' | Out-File '.targetframeworks'
}

$filter = $vs
if ($filter.Count -ne 0) {
  try {
    # Install required toolset
    . $PSScriptRoot/common/tools.ps1
    InitializeDotNetCli -install $true | Out-Null

    Push-Location $PSScriptRoot/../
    if ($filter -eq '*') {
      ./scripts/Slngen.ps1 -All -OutSln SDK.sln -NoLaunch
    }
    else {
      ./scripts/Slngen.ps1 $filter -OutSln SDK.sln -NoLaunch
    }

    if ($noLaunch -eq $false) {
      Write-Host "Launching Visual Studio..."
      Start-Process ./start-vs.cmd
      exit 0;
    }

    # We generated a new solution and we'll need to restore it before it's usable.
    $restore = $true;
  }
  catch {
    Write-Host $_.Exception.Message -Foreground "Red"
    Write-Host $_.ScriptStackTrace -Foreground "DarkGray"
    exit $global:LASTEXITCODE;
  }
  finally {
    Pop-Location
  }
}

# If no projects explicitly specified, look for a top-level solution file.
# - If there's no solution file found - no worries, build the default project configured in eng\Build.props.
# - If a solution file is found - buid it.
# - If more than one solution is found - fail.
if ([string]::IsNullOrWhiteSpace($projects)) {
  [object[]]$slnFiles = Get-ChildItem -Path $PSScriptRoot/../ -Filter *.sln;

  if ($slnFiles.Count -gt 1) {
    Write-Host "[ERROR] Multiple .sln files found in the root of the repository. Use '-projects' to specify the one you wish to build." -ForegroundColor Red;
    exit -1;
  }

  if ($slnFiles.Count -eq 1) {
    $projects = $slnFiles[0].FullName;
    Write-Host "[INFO] Building $projects..." -ForegroundColor DarkYellow
  }
  else {
    Write-Host "[INFO] Building the default project as configured in eng\Build.props..." -ForegroundColor Cyan
  }
}

. $PSScriptRoot/common/build.ps1 `
       -configuration $configuration `
       -platform $platform `
       -projects $projects `
       -verbosity $verbosity `
       -msbuildEngine $msbuildEngine `
       -warnAsError $([boolean]::Parse("$warnAsError")) `
       -nodeReuse $nodeReuse `
       -restore:$restore `
       -deployDeps:$deployDeps `
       -build:$build `
       -rebuild:$rebuild `
       -deploy:$deploy `
       -test:$test `
       -integrationTest:$integrationTest `
       -performanceTest:$performanceTest `
       -sign:$sign `
       -pack:$pack `
       -publish:$publish `
       -clean:$clean `
       -binaryLog:$binaryLog `
       -excludeCIBinarylog:$excludeCIBinarylog `
       -ci:$ci `
       -prepareMachine:$prepareMachine `
       -runtimeSourceFeed $runtimeSourceFeed `
       -runtimeSourceFeedKey $runtimeSourceFeedKey `
       -excludePrereleaseVS:$excludePrereleaseVS `
       -nativeToolsOnMachine:$nativeToolsOnMachine `
       -help:$help `
       @properties


# Perform code coverage as the last operation, this enables the following scenarios:
#   .\build.cmd -restore -build -c Release -testCoverage
if ($testCoverage) {
  try {
    # Install required toolset
    . $PSScriptRoot/common/tools.ps1
    InitializeDotNetCli -install $true | Out-Null

    Push-Location $PSScriptRoot/../

    $testResultPath = "./artifacts/TestResults/$configuration";

    # Run tests and collect code coverage
    ./.dotnet/dotnet dotnet-coverage collect --settings ./eng/CodeCoverage.config --output $testResultPath/local.cobertura.xml "build.cmd -test -configuration $configuration -bl:`$$binaryLog"

    # Generate the code coverage report and open it in the browser
    ./.dotnet/dotnet reportgenerator -reports:$testResultPath/*.cobertura.xml -targetdir:$testResultPath/CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
    Start-Process $testResultPath/CoverageResultsHtml/index.html
  }
  catch {
    Write-Host $_.Exception.Message -Foreground "Red"
    Write-Host $_.ScriptStackTrace -Foreground "DarkGray"
    exit $global:LASTEXITCODE;
  }
  finally {
    Pop-Location
  }
}
