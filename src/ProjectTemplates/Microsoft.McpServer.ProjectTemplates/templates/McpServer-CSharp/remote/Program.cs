var builder = WebApplication.CreateBuilder(args);

// Add the MCP services: the transport to use (http) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<RandomNumberTools>();

var app = builder.Build();
app.MapMcp();
#if (hostIdentifier == "vs")
app.UseHttpsRedirection();
#endif

app.Run();
