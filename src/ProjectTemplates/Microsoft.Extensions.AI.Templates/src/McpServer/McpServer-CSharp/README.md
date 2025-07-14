# MCP Server

This README was created using the C# MCP server template project. It demonstrates how you can easily create an MCP server using C# and then package it in a NuGet package.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

## Checklist before publishing to NuGet.org

- Test the MCP server locally using the steps below.
- Update the package metadata in the .csproj file, in particular the `<PackageId>`.
- Update `.mcp/server.json` to declare your MCP server's inputs.
  - See [configuring inputs](https://aka.ms/nuget/mcp/guide/configuring-inputs) for more details.
- Pack the project using `dotnet pack`.

The `bin/Release` directory will contain the package file (.nupkg), which can be [published to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package).

## Using the MCP Server

Once the MCP server package is published to NuGet.org, you can configure it in your preferred IDE. Both VS Code and Visual Studio use the `dnx` command to download and install the MCP server package from NuGet.org.

The configuration uses the following server definition:

```json
{
  "McpServer-CSharp": {
    "type": "stdio",
    "command": "dnx",
    "args": [
      "<your package ID here>",
      "--version",
      "<your package version here>",
      "--yes"
    ]
  }
}
```

### IDE-Specific Configuration

- **VS Code**: Add the above configuration to your VS Code user settings under a `"mcp"` → `"servers"` section. See [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers) for more information.
- **Visual Studio**: Add the above configuration to your Visual Studio MCP settings under a `"servers"` section. See [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers) for more information.

### Testing the MCP Server

Once configured, you can ask Copilot Chat for a random number, for example, `Give me 3 random numbers`. It should prompt you to use the `get_random_number` tool on the `McpServer-CSharp` MCP server and show you the results.

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.

The configuration uses the following server definition:

```json
{
  "McpServer-CSharp": {
    "type": "stdio",
    "command": "dotnet",
    "args": [
      "run",
      "--project",
      "<PATH TO PROJECT DIRECTORY>"
    ]
  }
}
```

### IDE-Specific Configuration

- **VS Code**: 
  - For workspace settings, create a `.vscode/mcp.json` file in your project directory with the above configuration under a `"servers"` section
  - For user settings, add the above configuration under a `"mcp"` → `"servers"` section with the full path to the project directory
- **Visual Studio**: Add the above configuration to your Visual Studio MCP settings under a `"servers"` section. You can use either a relative or full path to the project directory.
