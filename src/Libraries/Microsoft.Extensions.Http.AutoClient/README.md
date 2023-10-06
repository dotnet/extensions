# Microsoft.Extensions.Http.AutoClient

This package allows creating efficient HTTP clients for existing REST APIs with minimal code. The client code is generated at compile time, is highly efficient, and integrates with modern telemetry patterns.

## Getting started

### Install the package

```dotnetcli
dotnet add package Microsoft.Extensions.Http.AutoClient
```

## Usage Example

### Defining a client

The first step for generating an auto client, is to define an interface[^1] that describes the client. The interface must be marked with the `[AutoClient]` attribute.

```csharp
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    // ... client methods
}
```

By default, the generated client would be of type `MyDependencyClient` (Interface name without the `I`). If you need to use a different name for the dependency name, then you can use the different AutoClient constructors:

```csharp
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient", "MyCustomClientName")]
public interface IMyDependencyClient
{
    // ... client methods
}
```

### HTTP auto client methods

Each of the HTTP Auto Client interface methods must return a `Task<T>`, where T can be any of the following:

- `string`: If your return type is `Task<string>`, the raw content of the response, as a string, will be returned.
- `HttpResponseMessage`: If your return type is `Task<HttpResponseMessage>`, the result will be returned directly from the `HttpClient`'s `SendAsync` method.
- `Any serializable type`: If your return type is `Task<YourCustomType>`, the response content will be deserialized from JSON and returned.

If the content type of the response is not `application/json` and the method's return type is not `Task<string>`, an exception will be thrown. The Auto Clients do not support other content types yet. In the meantime, you may set the return type as `Task<string>` and perform the deserialization according to your requirements.

The HTTP method is defined from the attribute you use on the method. The following attributes are supported:

- GetAttribute
- PostAttribute
- PutAttribute
- PatchAttribute
- DeleteAttribute
- HeadAttribute
- OptionsAttribute

Each of these attributes must receive the path to call the API. This path must not contain query parameters. The path has to be relative as well, to be used along with the base address of the HttpClient.

The HTTP Auto Client methods must always have a `CancellationToken` as a parameter. This parameter is used to cancel the request if needed.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    [Get("/api/users")]
    Task<List<User>> GetUsers(CancellationToken cancellationToken = default);
}
```

### URL parameters

The URL may contain parameters. For example `/api/users/{userId}`. You can define these as parameters of the Auto Client method. The name of the parameters in the method and URL must match.

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    [Get("/api/users/{userId}")]
    public Task<User> GetUser(string userId, CancellationToken cancellationToken = default);
}
```

### HTTP Payloads

To send a payload with your request, you mus tuse the `BodyAttribute` on a method parameter. If you don't pass any parameter to it, it will consider the content type is JSON, serializing your parameter before sending. Otherwise, you can choose a `BodyContentType` and use it within the `BodyAttribute`.

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    [Post("/api/users")]
    public Task<User> CreateUserAsync([Body] User user);

    [Put("/api/users/{userId}/displayName")]
    public Task<User> UpdateDisplayName(string userId, [Body(BodyContentType.TextPlain)] string displayName, CancellationToken cancellationToken = default);
}
```

### Headers

There are two ways of sending headers with your HTTP request. One of them is best suited for headers that never change value and are static. The other is for headers that change based on the parameters of your Auto Client method.

#### Static Headers

With the `StaticHeaderAttribute`, you can define a static header to be used with every request from the Auto Client. You must pass the header name and value to its constructor.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
[StaticHeader("X-MyHeader", "HeaderValue")]
public interface IMyDependencyClient
{
    [Get("/api/users")]
    [StaticHeader("X-MethodHeader", "HeaderValue")]
    public Task<List<User>> GetUsers(CancellationToken cancellationToken = default);
}
```

#### Parameter Headers

With the `HeaderAttribute`, you can receive the value for a header from the attributes of your Auto Client method. You must pass the header name to its constructor.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    [Get("/api/users")]
    public Task<List<User>> GetUsers([Header("X-MyHeader")] string myHeader, CancellationToken cancellationToken = default);
}
```

### Query Parameters

Query parameters must be defined using the `QueryAttribute`. If not specified in the attribute's constructor, the parameter name will be used as the query key.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

[AutoClient("MyHttpClient")]
public interface IMyDependencyClient
{
    [Get("/api/users")]
    public Task<List<User>> GetUsers([Query] string search, [Query("namedQueryKey")] string namedKey, CancellationToken cancellationToken = default);
}
```

### Using the Auto Client

Along with the interface's implementation, extension methods will also be generated to help with registering the Auto Client in the DI container. The extension method will be named `Add{DependencyName}`. For example, if your interface is named `IMyDependencyClient`, the extension method will be `AddMyDependencyClient`.

```csharp
builder.Services
    .AddHttpClient("MyHttpClient", client =>    // <-- Adds a named HttpClient which will be matched with the parameter passed to the AutoClientAttribute.
    {
        client.BaseAddress = new Uri("https://myapi.com");
    });

builder.Services.AddMyDependencyClient();               // <-- Registers the Auto Client in the DI container.
```

After adding that, you can retrieve the Auto Client from the DI container and use it as you would with any other dependency.

```csharp
public class MyService
{
    private readonly IMyDependencyClient _myClient;

    public MyService(IMyDependencyClient myClient)
    {
        _myClient = myClient;
    }

    public async Task ProcessUsers()
    {
        var users = await _myClient.GetUsers();
        ...
    }
}
```

Or using minimal APIs:

```csharp
app.MapGet("/ProcessUsers", async ([FromServices] IMyDependencyClient client) =>
{
    return await client.GetUsers(CancellationToken.None);
});
```

[^1]: The attribute must be used in interfaces, not classes.

## Feedback & Contributing

For any feedback or contributions, please visit us in [our GitHub repo](https://github.com/dotnet/extensions).
