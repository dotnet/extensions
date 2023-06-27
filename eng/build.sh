#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

usage()
{
  echo "Custom settings:"
  echo "  --testCoverage             Run unit tests and capture code coverage information"
  echo "  --mutationTest             Run mutation tests"
  echo "  --vs <value>               Comma delimited list of keywords to filter the projects in the solution"
  echo "                             Pass '*' to generate a solution with all projects."
  echo "  --onlyTfms <value>         Semi-colon delimited list of TFMs to build (e.g. 'net8.0;net6.0')"
  echo ""
}

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT=$(realpath $DIR/../)

filter=false
keywords=''
onlyTfms=''
hasProjects=false
hasWarnAsError=false
hasRestore=false
configuration=''
testCoverage=false
mutationTest=false

properties=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -help|-h)
      usage
      "$DIR/common/build.sh" --help
      exit 0
      ;;
    -vs)
      filter=true
      shift
      keywords=$1
      ;;
    -onlytfms)
      shift
      onlyTfms=$1
      ;;
    -projects)
      hasProjects=true
      # Pass through resolving the full path to the project
      properties="$properties $1 $(realpath $2)"
      shift
      ;;
    -restore)
      hasRestore=true
      properties="$properties $1"
      ;;
    -warnaserror)
      hasWarnAsError=true
      # Pass through converting to boolean
      value=false
      if [[ "${2,,}" == "true" || "$2" == "1" ]]; then
        value=true
      fi
      properties="$properties $1 $value"
      shift
      ;;
    -configuration|-c)
      configuration=$2
      properties="$properties $1 $2"
      shift
      ;;
    -testcoverage)
      testCoverage=true
      ;;
    -mutationtest)
      mutationTest=true
      properties="$properties /p:TestRunnerName=StrykerNET"
      ;;
    *)
      properties="$properties $1"
      ;;
  esac

  shift
done

if [[ -n "${onlyTfms// /}" ]]; then
  echo $onlyTfms > ./.targetframeworks
fi

if [[ "$filter" == true ]]; then
  # Install required toolset
  . "$DIR/common/tools.sh"
  InitializeDotNetCli true > /dev/null

  # Invoke the solution generator
  script=$(realpath $DIR/../scripts/Slngen.ps1)

  if [[ "$keywords" == '*' ]]; then
    pwsh -Command "&{ $script -All -OutSln SDK.sln -NoLaunch }"
  else
    pwsh -Command "&{ $script -Keywords $keywords -OutSln SDK.sln -NoLaunch }"
  fi

  # We generated a new solution and we'll need to restore it before it's usable.
  if [[ "$hasRestore" == false ]]; then
    properties="$properties --restore"
  fi
fi

# If no projects explicitly specified, look for a top-level solution file.
# - If there's no solution file found - no worries, build the default project configured in eng\Build.props.
# - If a solution file is found - buid it.
# - If more than one solution is found - fail.
if [[ "$hasProjects" == false ]]; then
  repoRoot=$(realpath $DIR/../)
  fileCount=$(find $repoRoot -path "$repoRoot/*.sln" | wc -l)
  if [[ $fileCount > 1 ]]; then
    echo -e '\e[31m[ERROR] Multiple .sln files found in the root of the repository. Use '--projects' to specify the one you wish to build.\e[0m' >&2
    exit -1
  fi

  if [[ $fileCount == 1 ]]; then
    solution=$(realpath $(find $repoRoot/*.sln))
    echo -e "\e[33m[INFO] Building $solution...\e[0m"
    properties="$properties --projects $solution"
  else
    echo -e '\e[34m[INFO] Building the default project as configured in eng/Build.props...\e[0m'
  fi
fi

# The Arcade's default is "warnAsError=true", we want the opposite by default.
if [[ "$hasWarnAsError" == false ]]; then
  properties="$properties --warnAsError false"
fi

# If mutation testing is requested, ensure no incompatible switches supplied
if [[ "$mutationTest" == true ]]; then
  unsupportedSwitches=('restore' 'build' 'deploy' 'deploydeps' 'integrationtest' 'performancetest' 'sign' 'pack' 'testcoverage')
  for switch in "${unsupportedSwitches[@]}"; do
    if echo $properties | grep -cswi $switch > /dev/null; then
      echo "\e[31m[ERROR] Mutation testing is incompatible with '$switch' switch.\e[0m"
      echo "    Incompatible switches: ${unsupportedSwitches[*]// /|}"
      exit -1
    fi
  done

  requiredSwitches=('test')
  for switch in "${requiredSwitches[@]}"; do
    if echo $properties | grep -cswi $switch > /dev/null; then
      # switch is supplied
      echo "'$switch' switch is supplied" > /dev/null
    else
      properties="$properties --$switch"
    fi
  done

  # Set envvars so that Stryker can locate the .NET SDK
  export DOTNET_ROOT=$REPO_ROOT/.dotnet
  export DOTNET_MULTILEVEL_LOOKUP=0
  export PATH=$DOTNET_ROOT:$PATH

  # Create a marker file
  touch "$REPO_ROOT/.mutationtests"

  # Remove the marker upon failure
  trap 'rm "$REPO_ROOT/.mutationtests"' EXIT
fi

"$DIR/common/build.sh" $properties

# Remove the marker when we're done
if [[ "$mutationTest" == true ]]; then
  [ -e "$REPO_ROOT/.mutationtests" ] && rm -- "$REPO_ROOT/.mutationtests"
fi

# Perform code coverage as the last operation, this enables the following scenarios:
#   .\build.sh --restore --build --c Release --testCoverage
if [[ "$testCoverage" == true ]]; then
  # Install required toolset
  . "$DIR/common/tools.sh"
  InitializeDotNetCli true > /dev/null

  testResultPath="$REPO_ROOT/artifacts/TestResults/$configuration"

  # Run tests and collect code coverage
  $REPO_ROOT/.dotnet/dotnet 'dotnet-coverage' collect --settings $REPO_ROOT/eng/CodeCoverage.config --output $testResultPath/local.cobertura.xml "$REPO_ROOT/build.sh --test --configuration $configuration"

  # Generate the code coverage report and open it in the browser
  $REPO_ROOT/.dotnet/dotnet reportgenerator -reports:$testResultPath/*.cobertura.xml -targetdir:$testResultPath/CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
  echo ""
  echo -e "\e[32mCode coverage results:\e[0m $testResultPath/CoverageResultsHtml/index.html"
  echo ""
fi