# Microsoft.Extensions.AsyncState

This provides the ability to store and retrieve state objects that flow with the current asynchronous context.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AsyncState
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AsyncState" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Registering Services

The services can be registered using the following method:

```csharp
public static IServiceCollection AddAsyncState(this IServiceCollection services)
```

### Consuming Services

The `IAsyncContext<T>` can be injected wherever async state is needed. For example:

```csharp
public class MyClass
{
  public MyClass(IAsyncContext<MyState> asyncContext) { Context = asyncContext }

  private IAsyncContext<MyState> Context { get; }

  public async Task DoWork()
  {
    var state = Context.Get();
    // or
    Context.Set(new MyState());
    // or
    if (Context.TryGet(out var state)) { ... }
  }
}
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
