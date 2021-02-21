.NET Extensions
===============

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/extensions/Extensions-ci)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=23)

.NET Extensions is an open-source, cross-platform set of APIs for commonly used programming patterns and utilities, such as dependency injection, logging, and app configuration. Most of the API in this project is meant to work on many .NET platforms, such as .NET Core, .NET Framework, Xamarin, and others. While commonly used in ASP.NET Core applications, these APIs are not coupled to the ASP.NET Core application model. They can be used in console apps, WinForms and WPF, and others.

---

## *** Important Updates ***

As part of the [ongoing repository consolidation effort in .NET 5](https://github.com/dotnet/announcements/issues/119), we have moved most of the content from dotnet/extensions into other repositories. Most of these packages were developed by and are currently maintained by the ASP.NET team. However, moving forward we want to enable more scenarios with these packages, outside of ASP.NET. You can find more details on this in the [official announcement](https://github.com/aspnet/Announcements/issues/411) of the changes coming to extensions.

The full list of packages that have moved can be found below and in the [official announcement](https://github.com/aspnet/Announcements/issues/411). In general, tests and samples have moved to the relevant repo based on where the main package moved. Issue tracking for the relevant packages has also moved to that repository.

## Package List

The following list identifies all the packages we currently ship from [dotnet/extensions](https://github.com/dotnet/extensions) and which repo they have moved to.

* Moved to [dotnet/runtime](https://github.com/dotnet/runtime)
    * **Caching**
        * [`Microsoft.Extensions.Caching.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Caching.Abstractions)
        * [`Microsoft.Extensions.Caching.Memory`](https://nuget.org/packages/Microsoft.Extensions.Caching.Memory)
    * **Configuration**
        * [`Microsoft.Extensions.Configuration`](https://nuget.org/packages/Microsoft.Extensions.Configuration)
        * [`Microsoft.Extensions.Configuration.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions)
        * [`Microsoft.Extensions.Configuration.Binder`](https://nuget.org/packages/Microsoft.Extensions.Configuration.Binder)
        * [`Microsoft.Extensions.Configuration.CommandLine`](https://nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine)
        * [`Microsoft.Extensions.Configuration.EnvironmentVariables`](https://nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables)
        * [`Microsoft.Extensions.Configuration.FileExtensions`](https://nuget.org/packages/Microsoft.Extensions.Configuration.FileExtensions)
        * [`Microsoft.Extensions.Configuration.Ini`](https://nuget.org/packages/Microsoft.Extensions.Configuration.Ini)
        * [`Microsoft.Extensions.Configuration.Json`](https://nuget.org/packages/Microsoft.Extensions.Configuration.Json)
        * [`Microsoft.Extensions.Configuration.UserSecrets`](https://nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets)
        * [`Microsoft.Extensions.Configuration.Xml`](https://nuget.org/packages/Microsoft.Extensions.Configuration.Xml)
    * **Dependency Injection**
        * [`Microsoft.Extensions.DependencyInjection`](https://nuget.org/packages/Microsoft.Extensions.DependencyInjection)
        * [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions)
    * **File Providers**
        * [`Microsoft.Extensions.FileProviders.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions)
        * [`Microsoft.Extensions.FileProviders.Composite`](https://nuget.org/packages/Microsoft.Extensions.FileProviders.Composite)
        * [`Microsoft.Extensions.FileProviders.Physical`](https://nuget.org/packages/Microsoft.Extensions.FileProviders.Physical)
    * **File System Globbing**
        * [`Microsoft.Extensions.FileSystemGlobbing`](https://nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing)
    * **Hosting**
        * [`Microsoft.Extensions.Hosting`](https://nuget.org/packages/Microsoft.Extensions.Hosting)
        * [`Microsoft.Extensions.Hosting.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions)
        * [`Microsoft.Extensions.Hosting.Systemd`](https://nuget.org/packages/Microsoft.Extensions.Hosting.Systemd)
        * [`Microsoft.Extensions.Hosting.WindowsServices`](https://nuget.org/packages/Microsoft.Extensions.Hosting.WindowsServices)
    * **Http Client Factory**
        * [`Microsoft.Extensions.Http`](https://nuget.org/packages/Microsoft.Extensions.Http)
    * **Logging**
        * [`Microsoft.Extensions.Logging`](https://nuget.org/packages/Microsoft.Extensions.Logging)
        * [`Microsoft.Extensions.Logging.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
        * [`Microsoft.Extensions.Logging.Configuration`](https://nuget.org/packages/Microsoft.Extensions.Logging.Configuration)
        * [`Microsoft.Extensions.Logging.Console`](https://nuget.org/packages/Microsoft.Extensions.Logging.Console)
        * [`Microsoft.Extensions.Logging.Debug`](https://nuget.org/packages/Microsoft.Extensions.Logging.Debug)
        * [`Microsoft.Extensions.Logging.EventLog`](https://nuget.org/packages/Microsoft.Extensions.Logging.EventLog)
        * [`Microsoft.Extensions.Logging.EventSource`](https://nuget.org/packages/Microsoft.Extensions.Logging.EventSource)
        * [`Microsoft.Extensions.Logging.Testing`](https://nuget.org/packages/Microsoft.Extensions.Logging.Testing)
        * [`Microsoft.Extensions.Logging.TraceSource`](https://nuget.org/packages/Microsoft.Extensions.Logging.TraceSource)
    * **Options**
        * [`Microsoft.Extensions.Options`](https://nuget.org/packages/Microsoft.Extensions.Options)
        * [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions)
        * [`Microsoft.Extensions.Options.DataAnnotations`](https://nuget.org/packages/Microsoft.Extensions.Options.DataAnnotations)
    * **Primitives**
        * [`Microsoft.Extensions.Primitives`](https://nuget.org/packages/Microsoft.Extensions.Primitives)
* Moved to [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)
    * **Caching** (6.0 and beyond)
        * [`Microsoft.Extensions.Caching.SqlServer`](https://nuget.org/packages/Microsoft.Extensions.Caching.SqlServer)
        * [`Microsoft.Extensions.Caching.StackExchangeRedis`](https://nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis)
    * **Configuration**
        * [`Microsoft.Extensions.Configuration.KeyPerFile`](https://nuget.org/packages/Microsoft.Extensions.Configuration.KeyPerFile)
    * **File Providers**
        * [`Microsoft.Extensions.FileProviders.Embedded`](https://nuget.org/packages/Microsoft.Extensions.FileProviders.Embedded)
        * [`Microsoft.Extensions.FileProviders.Embedded.Manifest.Task`](https://nuget.org/packages/Microsoft.Extensions.FileProviders.Embedded.Manifest.Task)
    * **Health Checks**
        * [`Microsoft.Extensions.Diagnostics.HealthChecks`](https://nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks)
        * [`Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions)
    * **Http Client Factory** (6.0 and beyond)
        * [`Microsoft.Extensions.Http.Polly`](https://nuget.org/packages/Microsoft.Extensions.Http.Polly)
    * **JS Interop**
        * [`Microsoft.JSInterop`](https://nuget.org/packages/Microsoft.JSInterop)
        * [`Mono.WebAssembly.Interop`](https://nuget.org/packages/Mono.WebAssembly.Interop)
    * **Localization**
        * [`Microsoft.Extensions.Localization`](https://nuget.org/packages/Microsoft.Extensions.Localization)
        * [`Microsoft.Extensions.Localization.Abstractions`](https://nuget.org/packages/Microsoft.Extensions.Localization.Abstractions)
    * **Logging**
        * [`Microsoft.Extensions.Logging.AzureAppServices`](https://nuget.org/packages/Microsoft.Extensions.Logging.AzureAppServices)
    * **Object Pool**
        * [`Microsoft.Extensions.ObjectPool`](https://nuget.org/packages/Microsoft.Extensions.ObjectPool)
    * **Web Encoders**
        * [`Microsoft.Extensions.WebEncoders`](https://nuget.org/packages/Microsoft.Extensions.WebEncoders)
* Remaining in [dotnet/extensions](https://github.com/dotnet/extensions) for now
    * **Logging**
        * `Microsoft.Extensions.Logging.Analyzers` (has not been released to NuGet.org as of writing)
* No longer shipping in 5.0
    * [`Microsoft.Extensions.DiagnosticAdapter`](https://nuget.org/packages/Microsoft.Extensions.DiagnosticAdapter)
    * [`Microsoft.Extensions.DependencyInjection.Specification.Tests`](https://nuget.org/packages/Microsoft.Extensions.DependencyInjection.Specification.Tests)
* No longer shipping in 6.0
    * [`Microsoft.Extensions.Configuration.NewtonsoftJson`](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.NewtonsoftJson)

---

## Get Started

Follow the [Get Started](https://www.microsoft.com/net) guide for .NET to setup an initial .NET application.
Microsoft.Extensions APIs can then be added to the project using the [NuGet Package Manager](https://nuget.org).

## How to Engage, Contribute, and Give Feedback

Some of the best ways to contribute are to try things out, file issues, join in design conversations,
and make pull-requests.

* [Download our latest daily builds](./docs/DailyBuilds.md)
* [Build .NET Extensions from source code](./docs/BuildFromSource.md)
* Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## Reporting security issues and bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC)  secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx).

## Related projects

These are some other repos for related projects:

* [.NET Core](https://github.com/dotnet/core) - a cross-platform, open-source .NET platform
* [ASP.NET Core](https://github.com/dotnet/aspnetcore) - a .NET Core framework for building web apps
* [Entity Framework Core](https://github.com/dotnet/efcore) - data access technology

## Code of conduct

See [CODE-OF-CONDUCT](./CODE-OF-CONDUCT.md)

## Community forks

Some parts of this project have been forked by the community to add additional functionality:

#### [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)

This is a fork of Microsoft.Extensions.CommandLineUtils.

 - GitHub: <https://github.com/natemcmaster/CommandLineUtils>
 - NuGet: <https://www.nuget.org/packages/McMaster.Extensions.CommandLineUtils>
