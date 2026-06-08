open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open ModelContextProtocol.Server
open McpServer.Tools

[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder(args)

    // Add the MCP services: HTTP transport + tools
    builder.Services
        .AddMcpServer()
        .WithHttpTransport(fun options ->
            // Stateless mode recommended for servers that don't need
            // server-to-client requests like sampling or elicitation.
            options.Stateless <- true
        )
        .WithTools<RandomNumberTools>()
    |> ignore

    let app = builder.Build()

    app.MapMcp() |> ignore
    app.UseHttpsRedirection() |> ignore

    app.Run()

    0
