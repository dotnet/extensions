source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

repoRoot="$( cd "$scriptroot/.." && pwd )"
tmpDir="$repoRoot/artifacts/tmp"

if [ ! -d $tmpDir ]; then
    # 'mkdir -p' isn't technically guaranteed to be available on all POSIX systems
    # So if this fails, we just need to mk each dir separately.
    mkdir -p $tmpDir
fi

loggerRsp="$tmpDir/logger.rsp"
loggersZip="$tmpDir/msbuildlogger.zip"
loggerAssemblyName="Microsoft.TeamFoundation.DistributedTask.MSBuild.Logger.dll"
loggerAssembly="$tmpDir/$loggerAssemblyName"

# Until we figure out a better way to get this...
# This the same URL used by the Azure tasks: https://github.com/Microsoft/azure-pipelines-tasks/blob/da57c7efbf858a628786c869d7d14b887d1d90d7/Tasks/Common/MSBuildHelpers/make.json#L5
# And it's HTTPS, so I think we can trust it.
echo "Downloading Azure Pipelines MSBuild logger..."
curl -sSL https://vstsagenttools.blob.core.windows.net/tools/msbuildlogger/3/msbuildlogger.zip "-o$loggersZip" >/dev/null
echo "Extracting logger..."
unzip -p "$loggersZip" "$loggerAssemblyName" > "$loggerAssembly"

# Write a root "logdetail" entry
detailId=$(uuidgen)
detailStartTime=$(date -Is)
echo "##vso[task.logdetail id=$detailId;type=Process;name=Arcade Build;order=1;starttime=$detailStartTime;progress=0;state=Initialized;]"

loggerArg="-dl:CentralLogger,\"$loggerAssembly\";\"RootDetailId=$detailId|SolutionDir=$repoRoot\"*ForwardingLogger,\"$loggerAssembly\""

"$repoRoot/eng/common/cibuild.sh" "$@" "$loggerArg"
