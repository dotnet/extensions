# Build ASP.NET Core Tooling from Source

Building ASP.NET Core Tooling from source allows you to tweak and customize the Razor tooling experience for ASP.NET Core, and to contribute your improvements back to the project.

See <https://github.com/dotnet/aspnetcore/labels/area-razor.tooling> for known issues and to track ongoing work.

## Clone the source code

For a new copy of the project, run:

```ps1
git clone https://github.com/dotnet/aspnetcore-tooling.git
```

## Install pre-requisites

### Windows

Building ASP.NET Core Tooling on Windows requires:

* Windows 10, version 1803 or newer
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Git. <https://git-scm.org>
* NodeJS. LTS version of 10.14.2 or newer <https://nodejs.org>.
* Install yarn globally (`npm install -g yarn`)
* Python (3.\*) ([available via the Microsoft store](https://www.microsoft.com/en-us/p/python-38/9mssztt1n39l?activetab=pivot:overviewtab) or from <https://www.python.org/downloads/>).

### macOS/Linux

Building ASP.NET Core on macOS or Linux requires:

* If using macOS, you need macOS Sierra or newer.
* If using Linux, you need a machine with all .NET Core Linux prerequisites: <https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites>
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* curl <https://curl.haxx.se> or Wget <https://www.gnu.org/software/wget>
* Git <https://git-scm.org>
* NodeJS. LTS version of 10.14.2 or newer <https://nodejs.org>
* Python (3.\*) <https://www.python.org/downloads/>

**NOTE** some ISPs have been know to use web filtering software that has caused issues with git repository cloning, if you experience issues cloning this repo please review <https://help.github.com/en/github/authenticating-to-github/using-ssh-over-the-https-port>

## Building in Visual Studio

Before opening the `src/Razor/Razor.sln` file in Visual Studio or VS Code, you need to perform the following actions.

1. Executing the following on command-line:

   ```ps1
   .\restore.cmd
   ```

   This will download the required tools and build the entire repository once.

   > :bulb: Pro tip: you will also want to run this command after pulling large sets of changes. On the main
   > branch, we regularly update the versions of .NET Core SDK required to build the repo.
   > You will need to restart Visual Studio every time we update the .NET Core SDK.
   > To allow executing the setup script, you may need to update the execution policy on your machine.
   You can do so by running the `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` command
   in PowerShell. For more information on execution policies, you can read the [execution policy docs](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy).

2. Use the `startvs.cmd src/Razor/Razor.sln` script to open Visual Studio with the Razor solution. This script first sets the required
environment variables.

3. Set `Microsoft.VisualStudio.RazorExtension` as the startup project.

### Common error: Unable to locate the .NET Core SDK

Executing `.\restore.cmd` or `.\build.cmd` may produce these errors:

> error : Unable to locate the .NET Core SDK. Check that it is installed and that the version specified in global.json (if any) matches the installed version.
> error MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found.

In most cases, this is because the option _Use previews of the .NET Core SDK_ in VS2019 is not checked. Start Visual Studio, go to _Tools > Options_ and check _Use previews of the .NET Core SDK_ under _Environment > Preview Features_.


## Building with Visual Studio Code

Note, the [Visual Studio Code C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) is required.

Using Visual Studio Code with this repo requires setting environment variables on command line first.
Use these command to launch VS Code with the right settings.

On Windows (requires PowerShell):

```ps1
# The extra dot at the beginning is required to 'dot source' this file into the right scope.

. .\activate.ps1
code .
```

On macOS/Linux:

```bash
source activate.sh
code .
```

Note that if you are using the "Remote-WSL" extension in VSCode, the environment is not supplied
to the process in WSL.  You can workaround this by explicitly setting the environment variables
in `~/.vscode-server/server-env-setup`.
See https://code.visualstudio.com/docs/remote/wsl#_advanced-environment-setup-script for details.

## Building on command-line

You can also build the entire project on command line with the `build.cmd`/`.sh` scripts.

On Windows:

```ps1
.\build.cmd
```

On macOS/Linux:

```bash
./build.sh
```

By default, all of the C# projects are built. Some C# projects require NodeJS to be installed to compile JavaScript assets which are then checked in as source. If NodeJS is detected on the path, the NodeJS projects will be compiled as part of building C# projects. If NodeJS is not detected on the path, the JavaScript assets checked in previously will be used instead. To disable building NodeJS projects, specify `-noBuildNodeJS` or `--no-build-nodejs` on the command line.

### Using `dotnet` on command line in this repo

Because we are using pre-release versions of .NET Core, you have to set a handful of environment variables
to make the .NET Core command line tool work well. You can set these environment variables like this

On Windows (requires PowerShell):

```ps1
# The extra dot at the beginning is required to 'dot source' this file into the right scope.

. .\activate.ps1
```

On macOS/Linux:

```bash
source ./activate.sh
```

## Running tests on command-line

Tests are not run by default. Use the `-test` option to run tests in addition to building.

On Windows:

```ps1
.\build.cmd -test
```

On macOS/Linux:

```bash
./build.sh --test
```
