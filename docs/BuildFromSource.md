Build .NET Extensions from Source
=================================

Building .NET Extensions from source allows you tweak and customize the API, and
to contribute your improvements back to the project.

## Install prerequistes

### Windows

Building Extensions on Windows requires:

* Windows 7 or higher
* At least 5 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Visual Studio 2019. <https://visualstudio.com>
* Git. <https://git-scm.org>

### macOS/Linux

Building Extensions on macOS or Linux requires:

* If using macOS, you need macOS Sierra or newer.
* If using Linux, you need a machine with all .NET Core Linux prerequisites: <https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites>
* At least 5 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Git <https://git-scm.org>

## Building in Visual Studio

Visual Studio requires special tools and command line parameters. You can acquire these by executing the following on command-line:
```
.\restore.cmd
.\startvs.cmd
```
This will download required tools and start Visual Studio with the correct environment variables.

## Building on command-line

You can also build the entire project on command line with the `build.cmd`/`.sh` scripts.

On Windows:
```
.\build.cmd
```

On macOS/Linux:
```
./build.sh
```

## Using `dotnet` on command line in this repo

Because we are using pre-release versions of .NET Core, you have to set a handful of environment variables
to make the .NET Core command line tool work well. You can set these environment variables like this

On Windows (requires PowerShell):
```ps1
# The extra dot at the beginning is required to 'dot source' this file into the right scope.

. .\activate.ps1
```

On macOS/Linux:
```
source activate.sh
```

## Building with Visual Studio Code

Using Visual Studio Code with this repo requires setting environment variables on command line first.
Use these command to launch VS Code with the right settings.

On Windows (requires PowerShell):
```
. activate.ps1
code .
```

On macOS/Linux:
```
source activate.sh
code .
```

## Use the result of your build

After building Extensions from source, you can use these in a project by pointing NuGet to the folder containing the .nupkg files.

- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="MyBuildOfExtensions" value="C:\src\Extensions\artifacts\packages\Debug\Shipping\" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

- Update the versions on `PackageReference` items in your .csproj project file to point to the version from your local build.
  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging" Version="3.0.0-alpha1-t000" />
  </ItemGroup>
  ```

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).
