# Microsoft.AspNetCore.Testing

This package provides test fakes for integration testing of ASP.NET Core applications.

In particular:

- `IWebHostBuilder` extensions to setup the test web app.
- `IHost` extensions to access that test web app.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.AspNetCore.Testing
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Testing" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Creating a Test Web App

The [`IWebHostBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.hosting.iwebhostbuilder) extensions can help set up a host for testing.

```csharp
using var host = await FakeHost.CreateBuilder()
    .ConfigureWebHost(webHost => webHost.UseFakeStartup().ListenHttpOnAnyPort())
    .StartAsync();
```

### Accessing the test Web App

The [`IHost`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihost) extensions can help access the test host that was created above.

```csharp
using var client = host.CreateClient();

var response = await client.GetAsync("/");
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
