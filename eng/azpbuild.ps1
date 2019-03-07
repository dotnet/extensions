param(
    [string]$Configuration = "Release",
    [Parameter(ValueFromRemainingArguments = $true)][String[]]$OtherArgs
)

$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactDir = Join-Path $repoRoot "artifacts"
$tmpDir = Join-Path $artifactDir "tmp"
$loggerRsp = Join-Path $tmpDir "logger.rsp"
$loggersZip = Join-Path $tmpDir "msbuildlogger.zip"

if (!(Test-Path $tmpDir)) {
    mkdir $tmpDir | Out-Null
}

# Until we figure out a better way to get this...
# This the same URL used by the Azure tasks: https://github.com/Microsoft/azure-pipelines-tasks/blob/da57c7efbf858a628786c869d7d14b887d1d90d7/Tasks/Common/MSBuildHelpers/make.json#L5
# And it's HTTPS, so I think we can trust it.
$loggerUrl = "https://vstsagenttools.blob.core.windows.net/tools/msbuildlogger/3/msbuildlogger.zip"
Invoke-WebRequest $loggerUrl -OutFile $loggersZip
Expand-Archive $loggersZip -DestinationPath $tmpDir -Force
$loggerAssembly = Join-Path $tmpDir "Microsoft.TeamFoundation.DistributedTask.MSBuild.Logger.dll"

# Write a root "logdetail" entry
$detailId = [Guid]::NewGuid()
$detailStartTime = [datetime]::UtcNow.ToString('O')
"##vso[task.logdetail id=$detailId;type=Process;name=Arcade Build;order=1;starttime=$detailStartTime;progress=0;state=Initialized;]"

$loggerArg = "/dl:CentralLogger,`"$loggerAssembly`";`"RootDetailId=$($detailId)|SolutionDir=$($repoRoot)`"*ForwardingLogger,`"$loggerAssembly`""

# PowerShell has a lot of problems with strings with ";" in them when we keep passing them down through various layers
# So, let's write this to a temporary '.rsp' file
$loggerArg | Out-File $loggerRsp
$loggerRsp = Convert-Path $loggerRsp

eng\common\cibuild.cmd `
    -configuration $Configuration `
    -prepareMachine `
    "`@$loggerRsp" `
    @OtherArgs
