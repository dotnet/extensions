# Debugging VS4Mac

1. [Install latest VS4Mac](https://visualstudio.microsoft.com/vs/mac/). Can upgrade installed version to the [preview channel](https://docs.microsoft.com/en-us/visualstudio/mac/install-preview?view=vsmac-2019) if needed.
2. Open the Razor.sln
3. Under `Visual Studio -> Preferences -> SDK Locations -> .NET Core` update the Location to point to your local aspnetcore-tooling dotnet:
![image](https://user-images.githubusercontent.com/2008729/89470183-ff9aef00-d72f-11ea-9465-e57c46512e50.png)
4. Double-click on the `Microsoft.VisualStudio.Mac.RazorAddin` project and update the `Run Configuration: Default` section (`Start External Program` & `Environment Variables`):
![image](https://user-images.githubusercontent.com/2008729/89469594-98306f80-d72e-11ea-8ae3-4e652f9e75c6.png)

5. Right click the `Microsoft.VisualStudio.Mac.RazorAddin` project and `Set As Startup Project`.
6. Open the `~/Applications/Visual Studio.app/Contents/Resources/lib/monodevelop/bin/VisualStudio.exe.config` and update the binding redirects of the following to 42.42.42.42. This ensures WebTools can load our locally built assemblies so we need to update binding redirects. 
    - Microsoft.VisualStudio.Editor.Razor
    - Microsoft.CodeAnalysis.Razor.Workspaces
    Example:
    ![image](https://user-images.githubusercontent.com/2008729/89469968-7be10280-d72f-11ea-98af-d731c20167dd.png)
7. Go back to your Razor.sln and click run. You should now be able to hit breakpoints with your locally built code.
![image](https://user-images.githubusercontent.com/2008729/89470234-2bb67000-d730-11ea-924c-2ec9588b0af6.png)
