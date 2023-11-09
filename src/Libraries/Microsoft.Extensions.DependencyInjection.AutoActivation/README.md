# Microsoft.Extensions.DependencyInjection.AutoActivation

This provides the ability to instantiate registered singletons during startup instead of during the first time it is used.

A singleton is typically created when it is first used, which can lead to higher than usual latency in responding to incoming requests. Creating the instances on startup helps prevent the service from exceeding its SLA for the first set of requests it processes.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DependencyInjection.AutoActivation
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.AutoActivation" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Registering Services

The services to auto-activate can be registered using the following methods:

```csharp
static IServiceCollection ActivateSingleton<TService>(this IServiceCollection services);
static IServiceCollection ActivateSingleton(this IServiceCollection services, Type serviceType);
static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory);
static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services);
static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory);
static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services);
static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType);
static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
static IServiceCollection AddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType);
static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
static void TryAddActivatedSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
static void TryAddActivatedSingleton<TService>(this IServiceCollection services);
static void TryAddActivatedSingleton<TService, TImplementation>(this IServiceCollection services);
static void TryAddActivatedSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory);

static IServiceCollection ActivateKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey);
static IServiceCollection ActivateKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);
static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImplementation> implementationFactory);
static IServiceCollection AddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey);
static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory);
static IServiceCollection AddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey);
static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);
static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);
static IServiceCollection AddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);
static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey);
static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Type implementationType);
static void TryAddActivatedKeyedSingleton(this IServiceCollection services, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory);
static void TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey);
static void TryAddActivatedKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? serviceKey);
TryAddActivatedKeyedSingleton<TService>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory)
```

For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddActivatedSingleton<MyService>();

var app = builder.Build();

app.Run();

public class MyService
{
    public MyService()
    {
        Console.WriteLine("MyService is created");
    }
}
```

Result:

```
MyService is created
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5297
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Services that are already registered can also be auto-activated:

```csharp

builder.Services.AddSingleton<OtherService>();
// ...
builder.Services.ActivateSingleton<OtherService>();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
