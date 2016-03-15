EventNotification
===

Notice
-------

The infrastructure for publishing notifications has moved to the .NET Framework. See the new [`DiagnosticSource`](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSource.cs) and [`DiagnosticListener`](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticListener.cs) APIs in the `System.Diagnostics.DiagnosticSource` package. The infrastructure provided here is for subscribing to events using runtime-generated proxies.

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/t1bk7hrnqqvlx0fa/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/EventNotification/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/EventNotification.svg?branch=dev)](https://travis-ci.org/aspnet/EventNotification)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
