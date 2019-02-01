How to get daily builds of .NET Extensions
==========================================

Daily builds include the latest source code changes. They are not supported for production use and are subject to frequent changes, but we strive to make sure daily builds function correctly.

If you want to download the latest daily build and use it in a project, then you need to:

- Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/core-sdk#installers-and-binaries)
- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## NuGet packages

Daily builds of ackages can be found on <https://dotnet.myget.org/gallery/dotnet-core>. This feed may include
packages that will not be supported in a officially released build.

Commonly referenced packages:

[logging-myget]:  https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.Extensions.Logging
[logging-myget-badge]: https://img.shields.io/dotnet.myget/dotnet-core/vpre/Microsoft.AspNetCore.App.svg?style=flat-square&label=myget

[di-myget]:  https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.Extensions.DependencyInjection
[di-myget-badge]: https://img.shields.io/dotnet.myget/dotnet-core/vpre/Microsoft.AspNetCore.svg?style=flat-square&label=myget

Package                                    | MyGet
:------------------------------------------|:---------------------------------------------------------
Microsoft.Extensions.Logging               | [![][logging-myget-badge]][logging-myget]
Microsoft.Extensions.DependencyInjection   | [![][di-myget-badge]][di-myget]
