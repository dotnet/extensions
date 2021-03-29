# Debugging with experimental Roslyn bits
Sometimes it may be necessary to make changes in [`dotnet/roslyn`](https://github.com/dotnet/roslyn), and react to the changes in this repo. The following are steps which outline the general process in using Roslyn development `dll`s with Razor Tooling.

## Steps:
1. Checkout [`dotnet/roslyn`](https://github.com/dotnet/roslyn).
2. `./Restore.cmd`
3. Make the desired changes in `dotnet/roslyn`.
4. `./Build.cmd -pack`. The `-pack` option causes the creation of NuGet packages.
5. You should see the generated packages in the `roslyn\artifacts\packages\Debug\Release` directory. Take note of the package versions (ie. `Microsoft.CodeAnalysis.Workspaces.Common.3.8.0.nupkg` => `3.8.0`).
6. Open `aspnetcore-tooling/NuGet.config` and add the local package source `<add key="Roslyn Local Package source" value="<PATH_TO_ROSLYN_REPO>\artifacts\packages\Debug\Release" />`.
7. Open `aspnetcore-tooling/eng/Versions.props` and update all the `Tooling_*` versions to the version noted in step 5.

## Notes:
- If you're familiar with _Visual Studio Hives_ the `dotnet/roslyn` project uses the `RoslynDev` root suffix .
- [Building Roslyn on Windows](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building,%20Debugging,%20and%20Testing%20on%20Windows.md)
- [Building Roslyn on Linux and Mac](https://github.com/dotnet/roslyn/blob/main/docs/infrastructure/cross-platform.md)
