# Debugging with experimental ASP.NET Core bits
Sometimes it may be necessary to make changes in [`dotnet/aspnetcore`](https://github.com/dotnet/aspnetcore), and react to the changes in this repo. The following are steps which outline the general process in using ASP.NET Core development `nupkg`s with Razor Tooling.

## Steps:
1. Checkout [`dotnet/aspnetcore`](https://github.com/dotnet/aspnetcore).
2. `./restore.cmd`
3. Make the desired changes in `dotnet/roslyn`.
4. `./build.cmd -pack`. The `-pack` option causes the creation of NuGet packages.
5. You should see the generated packages in the `aspnetcore\artifacts\packages\Debug\NonShipping` directory. The packages should end with `5.0.0-dev.nupkg`.
6. Open `aspnetcore-tooling/NuGet.config` and add the local package source `<add key="ASPNETCORE" value="<PATH_TO_ASPNET_CORE_REPO>\artifacts\packages\Debug\NonShipping\" />`.
7. Open `aspnetcore-tooling/eng/Versions.props` and note the version for `MicrosoftCodeAnalysisRazorPackageVersion`. Ex. `5.0.0-rc.1.20380.7`.
8. Do a find in `Versions.props` for the version in step 7 and replace with `5.0.0-dev.nupkg`.

## Notes:
- ⚠️ Ensure you do not commit the changes to `aspnetcore-tooling/NuGet.config` & `aspnetcore-tooling/eng/Versions.props`!
