open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open McpServer.Tools

[<EntryPoint>]
let main args =

    let builder = Host.CreateApplicationBuilder(args)

    // Configure logging to stderr
    builder.Logging.AddConsole(fun o ->
        o.LogToStandardErrorThreshold <- LogLevel.Trace
    )
    |> ignore

    // Add MCP services, stdio transport, and tools
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<RandomNumberTools>()
    |> ignore

    builder.Build().RunAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0
