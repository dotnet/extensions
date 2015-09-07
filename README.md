Testing
=======
AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/nwh8qlyaisvg3im5/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/Testing/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/Testing.svg?branch=dev)](https://travis-ci.org/aspnet/Testing)

This repository contains testing infrastructure for ASP.NET

To get started using it, see [How to create test projects](https://github.com/aspnet/Testing/wiki/How-to-create-test-projects).

#### Troubleshooting

If you're using xUnit, make sure that you're using a versions of `xunit` and `xunit.runner.dnx` that are compatible with your dnx version.

For dnx 1.0.0-beta7: reference `xunit 2.1.0-rc1-*` and `xunit.runner.dnx 2.1.0-beta5-*`.
For dnx 1.0.0-beta8: reference `xunit 2.1.0-*` and `xunit.runner.dnx 2.1.0-*`.

If you're having trouble running tests in Visual Studio, try at the commandline first:
```
cd <project directory>
dnx test
```

If everything is working at the commandline, look at the VS Output Window. Setting the environment variable `VS_UTE_DIAGNOSTICS = 1` may provide additional information.

If you see a text in the output window like:
```
System.InvalidOperationException: Unable to load application or execute command 'Microsoft.Dnx.TestHost'. Available commands: test.
   at Microsoft.Dnx.ApplicationHost.Program.ThrowEntryPointNotfoundException(DefaultHost host, String applicationName, Exception innerException)
   at Microsoft.Dnx.ApplicationHost.Program.ExecuteMain(DefaultHost host, String applicationName, String[] args)
   at Microsoft.Dnx.ApplicationHost.Program.Main(String[] args)
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at Microsoft.Dnx.Runtime.Common.EntryPointExecutor.Execute(Assembly assembly, String[] args, IServiceProvider serviceProvider)
   at Microsoft.Dnx.Host.Bootstrapper.RunAsync(List`1 args, IRuntimeEnvironment env, FrameworkName targetFramework)
   at Microsoft.Dnx.Host.RuntimeBootstrapper.ExecuteAsync(String[] args, FrameworkName targetFramework)
   at Microsoft.Dnx.Host.RuntimeBootstrapper.Execute(String[] args, FrameworkName targetFramework)
Unable to start Microsoft.Dnx.TestHost
```

Or

```
System.MissingMethodException: Method not found: 'Boolean Microsoft.Framework.Runtime.Project.TryGetProject(System.String, Microsoft.Framework.Runtime.Project ByRef, System.Collections.Generic.ICollection1)'. at Microsoft.Framework.TestHost.Program.<>c__DisplayClass2_0.<b__0>d.MoveNext() at System.Runtime.CompilerServices.AsyncTaskMethodBuilder1.StartTStateMachine
at Microsoft.Framework.TestHost.Program.<>c__DisplayClass2_0.b__0()
```

Then you almost certainly have a compatability problem caused by mixing package versions spanning separate beta releases. Doing a `dnu list` on your project at the commandline will help pin down what version is incompatible.

This project is part of ASP.NET 5. You can find samples, documentation and getting started instructions for ASP.NET 5 at the [Home](https://github.com/aspnet/home) repo.
