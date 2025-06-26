# MCP Server

This README was created using the .NET MCP server template project. It demonstrates how you can easily create an MCP server using .NET and then package it in a NuGet package.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

## Checklist before publishing to NuGet.org

- Update package metadata in the .csproj file
- Update the `.mcp/server.json` to declare your MCP server's inputs
- Test the MCP server locally using the steps below 

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, use the following MCP configuration in VS Code. See [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers) for more information about using MCP servers in VS Code.

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

Then ask the Copilot chat for a random number. It should prompt to use the `GetRandomNumber` tool on the `my-custom-mcp` MCP server. 
