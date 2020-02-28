# Using the Razor LSP Editor

To use the Razor LSP editor set an environment variable under the name `Razor.LSP.Editor` to `true` and then open Razor.sln. Running the `Microsoft.VisualStudio.RazorExtension` project will then result in `.razor` and `.cshtml` files being opened with our LSP editor.

To set the environment variable in powershell you can use the following syntax: `${env:Razor.LSP.Editor}="true"`

# FAQ

**Opening a project results in my Razor file saying "waiting for IntelliSense to initialize", why does it never stop?**
This is a combo issue dealing with how Visual Studio serializes project state after a feature flag / environment variable has been set. Basically, prior to setting `Razor.LSP.Editor` Visual Studio will have serialized project state that says a Razor file was opened with the WTE editor. Therefore, when you first open a project that Razor file will attempt to be opened under the WTE editor but the core editor will conflict saying it should be opened by our editor. This results in the endless behavior of "waiting for IntelliSense to initialize".

Close and re-open the file and it shouldn't re-occur if you re-save the solution.