# Microsoft.AspNetCore.Razor.VSCode.BlazorWasmDebuggingExtension

**Warning:** This extension is indeed for use alongside the C# extension.

This directory contains the code for the Blazor WebAssembly Debugging extension. This extension is designed to support debugging scenarios for Blazor WebAssembly.

Blazor WebAssembly runs in the browser via a .NET runtime running on WebAssembly. As a result, the traditional infrastructure that is used to debug .NET applications cannot be used for Blazor WebAssembly. Instead, Blazor WebAssembly leverages a debugging proxy that communicates with a web browser and IDE over the Chrome DevTools Protocol.

This debugging proxy is typically launched by the the Blazor development server wherever the Blazor server is running. This experience works fine for "local dev" scenarios where the developer is running their browser, the IDE, and the Blazor development server on the same machine.

In remote debugging scenarios, the browser, IDE, and server hosting the Blazor app are running on different machines.

- The browser runs on the user's host machine.
- The server hosting the Blazor app and debugging proxy run in the remote environment (Docker container, WSL, Codespaces)
- VS Code runs in a dual-mode with some extension (UI extensions) running on the host machine and some extensions running on the remote machine (workspace extensions)

The debugging proxy needs to talk to both VS Code and the browser but they run in different places. In particular, the debugging proxy cannot communicate with the browser from inside the remote machine.

To that end, we need a way to launch the debugging proxy on the user's host machine, where the browser is running. To achieve this, we developed a UI-only extension that runs on the VS Code UI (the same place as the users host) and is responsible for launching and killing a Blazor debugging proxy on the host machine.

### Publishing the extension

In order to publish the extension, you will need to have access to the `ms.dotnet-tools` publisher account on the VS Code marketplace. If you don't already have this access, reach out to @captainsafia for info.

This extension bundles the debugging proxy assets that are needed inside the `BlazorDebugProxy` directory. These assets are not committed to repository so they will need to be included as part of the publish process.

1. Generate a personal access token per the instructions in [the VS Code publishing guide](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=preview-page).
2. Store the token from #1 in the `VSCODE_MARKETPLACE_TOKEN` environment variable.
3. Increment the `patch` version of the package in the `package.json` file.
4. Open a PR to the aspnetcore-tooling repo.
5. Download the VSIX asset from the artifiacts of the build.
6. Publish the VSIX asset using `vsix publish --packagePath ${pathFrom5} -p $VSCODE_MARKETPLACE_TOKEN`.
