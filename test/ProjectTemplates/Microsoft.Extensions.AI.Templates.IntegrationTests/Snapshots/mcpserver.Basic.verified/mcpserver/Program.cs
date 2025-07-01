using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mcpserver.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Uncomment the following lines if you want to enforce the MCP command-line arguments.
// You can use command-line arguments to support multiple modes or commands in your application.
// These would be specified in the "package_arguments" property in the .mcp/server.json to inform client tools.
/*
if (args.Length == 0 || args[0] != "mcp")
{
    Console.Error.WriteLine("Error: invalid command. Use the 'mcp' command-line argument to start the MCP server.");
    return 1;
}
*/

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>();

await builder.Build().RunAsync();
