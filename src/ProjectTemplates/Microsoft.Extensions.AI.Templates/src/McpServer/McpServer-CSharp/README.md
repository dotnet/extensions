# MCP Server

This README was created using the .NET MCP server template project. It demonstrates how you can easily create an MCP server using .NET and then package it in a NuGet package.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

## Checklist before publishing to NuGet.org

- Update the package metadata in the .csproj file
- Update `.mcp/server.json` to declare your MCP server's inputs
- Test the MCP server locally using the steps below

## Using the MCP Server in VS Code

Once the MCP server package is published to NuGet.org, you can use the following VS Code user configuration to download and install the MCP server package. See [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers) for more information about using MCP servers in VS Code.

```json
{
  "mcp": {
    "servers": {
      "my-custom-mcp": {
        "type": "stdio",
        "command": "dotnet",
        "args": [
          "tool",
          "exec",
          "<your package ID here>",
          "--version",
          "<your package version here>",
          "--yes",
          "--",
          "start-mcp"
        ],
        "env": {
          "MAX_RANDOM_NUMBER": 100
        }
      }
    }
  }
}
```

Now you can ask Copilot Chat for a random number, for example, `Give me 3 random numbers`. It should prompt you to use the `GetRandomNumber` tool on the `my-custom-mcp` MCP server and show you the results.

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, use the following VS Code configuration:

```json
{
  "mcp": {
    "servers": {
      "my-custom-mcp": {
        "type": "stdio",
        "command": "dotnet",
        "args": [
          "run",
          "--project",
          "<PATH TO PROJECT DIRECTORY>",
          "--",
          "start-mcp"
        ],
        "env": {
          "MAX_RANDOM_NUMBER": 100
        }
      }
    }
  }
}
```
