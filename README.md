# Razor.VSCode

The [Razor syntax](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor) provides a fast, terse, clean and lightweight way to combine server code with HTML to create dynamic web content. This repo contains the tooling to enable Razor in Visual Studio Code. The Razor tooling ships as part of the [Visual Studio Code C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp).

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## Get started

To use the Razor tooling for Visual Studio Code:
- Install [Visual Studio Code](https://code.visualstudio.com) + [latest C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- Open any project containing Razor (cshtml) files to get intellisense and diagnostics:

  ![Razor tooling for Visual Studio Code](https://user-images.githubusercontent.com/1874516/46520947-c0996b00-c832-11e8-9860-9c4490a90fe5.gif)

## Disabling the Razor tooling

To disable the Razor tooling in Visual Studio Code:
- Open the Visual Studio Code User Settings: *File* -> *Preferences* -> *Settings*
- Search for "razor"
- Check the "Razor: Disabled" checkbox

## Building from source

Prerequisites:
- [Visual Studio Code](https://code.visualstudio.com) + [latest C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [Node.js](https://nodejs.org) (8.12.0 or later)
- Clode the repo and restore Git submodules by running the following:

  ```
  git clone https://github.com/aspnet/Razor.VSCode.git
  cd Razor.VSCode
  git submodule update --init --recursive
  ```

This repo uses the same set of build tools as the other ASP.NET Core projects. The [developer documentation](https://github.com/aspnet/Home/wiki/Building-from-source) for building is the authoritative guide. **Please read this document and check your PATH setup if you have trouble building or using Visual Studio Code**

To build at the command line, run `build.cmd` or `build.sh` from the solution directory.

## Run in Visual Studio Code

To run the built Razor tooling in Visual Studio Code: 

- In the `client` directory run the following:

  ```
  npm install
  npm run compile
  ```

- Open the solution folder in Visual Studio Code
- Set the "razor.disabled" setting to true in the Visual Studio Code Workspace settings (*File* -> *Preferences* -> *Settings* -> *Workspace Settings*)
- Run in the debugger (F5) using the *Extension* launch profile
- A new Visual Studio Code instance will open as an Extension Development Host
- Try out Razor tooling features in `Pages/Index.cshtml` or any other Razor file
  - NOTE: there may be a delay while the Razor Language Service starts up. See the *Razor Log* and *OmniSharp log* in the output window (*View* -> *Output*) to see the current status

## Run all extension tests

To run all extension tests:
- Close all instances of Visual Studio Code
- From the `client` directory run:

  ```
  npm test
  ```
