Common
======
AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/snawy2a2vt0vd7dv/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/Common/branch/dev)
Travis:   [![Travis](https://travis-ci.org/aspnet/Common.svg?branch=dev)](https://travis-ci.org/aspnet/Common)

The Common repository includes projects containing commonly used primitives and utility types.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## About this branch

The servicing/1.0.x branch was created to support the "Source Build" effort for .NET Core: https://github.com/dotnet/source-build.
Because the NuGet client in the 2.0.0 SDK took a dependency on Microsoft.Extensions.CommandLineUtils 1.0.1, we had to upgrade this
repo from project.json to build with csproj. This presence of this branch does not represent the intention to ship new versions, only
to support building existing ones from source only.
