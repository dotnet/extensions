# Build instructions

- [Introduction](#introduction)
- [Ubuntu](#ubuntu)
  * [Building from command line](#building-from-command-line)
  * [Building from Visual Studio Code](#building-from-visual-studio-code)
- [Windows](#windows)
  * [Building from command line](#building-from-command-line-1)
  * [Building from Visual Studio](#building-from-visual-studio)
  * [Building from Visual Studio Code](#building-from-visual-studio-code-1)
- [Build outputs](#build-outputs)
- [Troubleshooting build errors](#troubleshooting-build-errors)

## Introduction

Like many other .NET repositories, this repository uses the [.NET Arcade SDK][arcade-sdk]. With that, most repository interactions are facilitated with `.\eng\common\build.cmd` or `.\eng\common\build.sh`. We added helper scripts to simplify some of the most common interactions with those, such as `restore.cmd`/`restore.sh` and `build.cmd`/`build.sh`.

Also, like all the Arcadified repositories, this repository does **not** build using your machine-wide installed version of the .NET SDK. Instead it builds using the repo-local .NET SDK specified in the [`global.json`](..\global.json) file located in the repository root.<br />
:warning: This also means that you **cannot** double click on any of the projects or a solution file in this repository and open those in Visual Studio. Generally, it won't work as Visual Studio won't be able to resolve the required .NET SDK. You will need to use our helper scripts provided for your convenience.

However, unlike pretty much all .NET repositories, this repository does not contain a solution file due to the number of projects in the solutions and the target framework monikers (TFMs) those projects use. It's very unlikely that you, as a developer, will be dealing with all projects in this repository at the same time. Instead, we expect that you will work on a subset of projects, using a "filtered" solution generated with the [slngen tool][slngen-tool]. Please read below for detailed instructions.

## Ubuntu

### Building from command line

#### TL;DR

Building the solution is as easy as running:

```bash
$ ./build.sh
```

#### Various scripts to build and test the solution

The repo provides the following helper scripts for your convenience:

* `restore.sh` - this script will install the required .NET SDK, .NET tools and the toolset.<br />
This script is equivalent to running `./build.sh --restore`.

* `build.sh` - this is a "one-stop shop" script that accepts a whole plethora of commands.<br />
Here are few commands that you will likely use the most:
    - `build.sh`: without any parameters this is equivalent to running `./build.sh --restore --build`.
    - `build.sh --restore`: to install the required .NET SDK, .NET tools and the toolset. This is equivalent to running `./restore.sh`.
    - `build.sh --build`: to build the solution<sup>1</sup>.
    - `build.sh --test`: to run all unit tests in the solution<sup>1</sup>.
    - `build.sh --vs <keywords>`: to generate a "filtered" solution and save it as `SDK.sln`. It also performs the "restore" operation. For example: `./build.sh --vs Http,Fakes`.<br />
    If for some reason you wish to generate a solution with all projects you can pass `*` for the keyword, e.g.: `./build.sh --vs '*'` (Note: you have to escape the asterisk or use `set -f` to turn off expansion).<br />
      > Under the hood, this invokes `scripts/Slngen.ps1` script, which in turn executes [slngen tool][slngen-tool]. If you want to customize how the "filtered" solution is generated, you will need to invoke `scripts/Slngen.ps1` script directly.<br />
        Run `./scripts/Slngen.ps1 -help` for more details.

    To find out more about the script and its parameters run: `./build.sh --help`.

* `start-code.sh` - this script sets up necessary environmental variables and opens the repository in VS Code, so that you can interact with the repository in "dotnet"-way, that is run `dotnet` commands from the VS Code's terminal.

### Building from Visual Studio Code

To open the solution in VS Code run the following command:

```bash
$ ./start-code.sh
```

This sets up necessary environmental variables and opens the repository in VS Code. It is advisable that you continue to interact with the solution (i.e., restore, build or test) via the [helper scripts described above](#building-from-command-line), however, it should be generally possible to interact with the repository in "dotnet"-way, that is run `dotnet` commands from the VS Code's terminal.


## Windows

### Building from command line

#### TL;DR

Building the solution is as easy as running:

```bash
> build.cmd
```

#### Various scripts to build and test the solution

* `restore.cmd` - this script will install the required .NET SDK, .NET tools and the toolset.<br />
This script is equivalent to running `.\build.cmd -restore`.
* `build.cmd` - this is a "one-stop shop" script that accepts a whole plethora of commands.<br />
Here are few commands that you will likely use the most:
    - `build.cmd`: without any parameters this is equivalent to running `.\build.cmd -restore -build`.
    - `build.cmd -restore`: to install the required .NET SDK, .NET tools and the toolset. This is equivalent to running `.\restore.cmd`.
    - `build.cmd -build`: to build the solution<sup>1</sup>.
    - `build.cmd -test`: to run all unit tests in the solution<sup>1</sup>.
    - `build.cmd -vs <keywords>`: to generate a "filtered" solution, save it as `SDK.sln` and then open in Visual Studio. For example: `.\build.cmd -vs Http,Fakes`.<br />
    If for some reason you wish to generate a solution with all projects you can pass `*` for the keyword, e.g.: `.\build.cmd -vs *`.<br />
      > Under the hood, this invokes `scripts\Slngen.ps1` script, which in turn executes [slngen tool][slngen-tool]. If you want to customize how the "filtered" solution is generated, you will need to invoke `scripts\Slngen.ps1` script directly.<br />
        Run `.\scripts\Slngen.ps1 -help` for more details.

To find out more about the script and its parameters run: `.\build.cmd -help`.

### Building from Visual Studio

#### TL;DR

Generating a new "filtered" solution and opening it in Visual Studio is as easy as running:

```powershell
> build.cmd -vs <keywords>
```

For example, to generate a solution that contains projects with "Http" and "Fakes" in their names you can run: `.\build.cmd -vs Http,Fakes`.<br />
If for some reason you wish to generate a solution with all projects you can pass `*` for the keyword, e.g.: `.\build.cmd -vs *`.

If you already have a solution you'd like to open in Visual Studio then run the following command:

```powershell
> start-vs.cmd
```

#### Various scripts to build and test the solution

1. If you don't have a solution file, or you wish to generate a new one: run `.\restore.cmd -vs <keywords>`.
1. If you have a solution file already (e.g., SDK.sln), then you can simply run `.\start-vs.cmd`.
1. (Advanced) If you want to customize how the "filtered" solution is generated, you will need to invoke `scripts\Slngen.ps1` script directly followed by `start-vs.cmd`.<br />
   Run `.\scripts\Slngen.ps1 -help` for more details.

### Building from Visual Studio Code

To open the solution in VS Code run the following command:

```powershell
> start-code.cmd
```

This sets up necessary environmental variables and opens the repository in VS Code. It is advisable that you continue to interact with the solution (i.e., restore, build or test) via the [helper scripts described above](#building-from-command-line-1), however, it should be generally possible to interact with the repository in "dotnet"-way, that is run `dotnet` commands from the VS Code's terminal.


## Build outputs

* All build outputs are generated under the `artifacts` folder.
* Binaries are under `artifacts\bin`.
* Logs are found under `artifacts\log`.
* Packages are found under `artifacts\packages`.


## Troubleshooting build errors

* Most build errors are compile errors and can be dealt with accordingly.
* Other error may be from MSBuild tasks. You need to examine the build logs to investigate.
  * The logs are generated at `.\artifacts\log\Debug\Build.binlog`
  * The file format is an MSBuild Binary Log. Install the [MSBuild Structured Log Viewer][msbuild-log-viewer] to view them.
* Windows Forms uses Visual Studio MSBuild but for certain features we require the latest MSBuild from .NET Core/.NET SDK. If you are on an official version of [Visual Studio][VS-download] (i.e. not a Preview version), then you may need to enable previews for .NET Core/.NET SDKs in VS.
  * you can do this in VS under Tools->Options->Environment->Preview Features->Use previews of the .Net Core SDK (Requires restart)
* When restoring packages, if you get the following message: "The SSL connection could not be established, see inner exception. Unable to read data from the transport connection", try disabling IPv6 on your network adapter.

---

1. **"Solution"** means the collections of projects specified in `eng/build.proj` or an actual "sln" file at the root of the repository that represents the generated "filtered" solution (e.g., `SDK.sln`).

[comment]: <> (URI Links)

[arcade-sdk]: https://github.com/dotnet/arcade/blob/main/Documentation/Overview.md
[msbuild-log-viewer]: https://msbuildlog.com/
[slngen-tool]: https://github.com/microsoft/slngen
[VS-download]: https://visualstudio.microsoft.com/downloads/

