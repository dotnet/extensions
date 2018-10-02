Logging
=======

**Package**: `Microsoft.Extensions.Logging`
NuGet (`master`): [![](http://img.shields.io/nuget/v/Microsoft.Extensions.Logging.svg?style=flat-square)](http://www.nuget.org/packages/Microsoft.Extensions.Logging) [![](http://img.shields.io/nuget/dt/Microsoft.Extensions.Logging.svg?style=flat-square)](http://www.nuget.org/packages/Microsoft.Extensions.Logging)
MyGet (`dev`): [![](http://img.shields.io/dotnet.myget/aspnetcore-dev/v/Microsoft.Extensions.Logging.svg?style=flat-square)](https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.Extensions.Logging)

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/i0hdtuq4m6pwfp2s/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/Logging/branch/dev)
Travis:   [![Travis](https://travis-ci.org/aspnet/Logging.svg?branch=dev)](https://travis-ci.org/aspnet/Logging)

Common logging abstractions and a few implementations. Refer to the [wiki](https://github.com/aspnet/Logging/wiki) for more information

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## Providers

Community projects adapt _Microsoft.Extensions.Logging_ for use with different back-ends.

 * [Sentry](https://github.com/getsentry/sentry-dotnet) - provider for the [Sentry](https://github.com/getsentry/sentry) library
 * [Serilog](https://github.com/serilog/serilog-framework-logging) - provider for the Serilog library
 * [elmah.io](https://github.com/elmahio/Elmah.Io.Extensions.Logging) - provider for the elmah.io service
 * [Loggr](https://github.com/imobile3/Loggr.Extensions.Logging) - provider for the Loggr service
 * [NLog](https://github.com/NLog/NLog.Extensions.Logging) - provider for the NLog library
 * [Graylog](https://github.com/mattwcole/gelf-extensions-logging) - provider for the Graylog service
 * [Sharpbrake](https://github.com/airbrake/sharpbrake#microsoftextensionslogging-integration) - provider for the Airbrake notifier
 * [KissLog.net](https://github.com/catalingavan/KissLog-net) - provider for the KissLog.net service

## Building from source

To run a complete build on command line only, execute `build.cmd` or `build.sh` without arguments. See [developer documentation](https://github.com/aspnet/Home/wiki) for more details.
