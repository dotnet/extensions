# Having a "proper" VS build
1. From an Administrator Powershell prompt run \\vspreinstall\PREINSTALL\Preinstall.cmd
1. Install the VS Enterprise->Branch Channel of Visual Studio from the latest successful build of http://ddweb/dashboard/vsbuild/.
1. Workloads: ASP.NET and web dev., VS extension dev., .NET Core cross-platform dev., .NET desktop Dev.

# Using the Razor LSP Editor

To use the Razor LSP editor set an environment variable under the name `Razor.LSP.Editor` to `true` and then open Razor.sln. Running the `Microsoft.VisualStudio.RazorExtension` project will then result in `.razor` and `.cshtml` files being opened with our LSP editor.

To set the environment variable in powershell you can use the following syntax: `${env:Razor.LSP.Editor}="true"`

# FAQ

**Opening a project results in my Razor file saying "waiting for IntelliSense to initialize", why does it never stop?**
This is a combo issue dealing with how Visual Studio serializes project state after a feature flag / environment variable has been set. Basically, prior to setting `Razor.LSP.Editor` Visual Studio will have serialized project state that says a Razor file was opened with the WTE editor. Therefore, when you first open a project that Razor file will attempt to be opened under the WTE editor but the core editor will conflict saying it should be opened by our editor. This results in the endless behavior of "waiting for IntelliSense to initialize".

Close and re-open the file and it shouldn't re-occur if you re-save the solution.