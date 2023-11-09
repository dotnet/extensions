# Microsoft.Extensions.TimeProvider.Testing

Provides a `FakeTimeProvider` for testing components that depend on `System.TimeProvider`.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.TimeProvider.Testing
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

`FakeTimeProvider` can be used to manually adjust time to test time dependent components in a deterministic way.

`FakeTimeProvider` derives from [TimeProvider](https://learn.microsoft.com/dotnet/api/system.timeprovider) and adds the following APIs:

```csharp
public FakeTimeProvider(DateTimeOffset startDateTime)
public DateTimeOffset Start { get; }
public TimeSpan AutoAdvanceAmount { get; set; }
public void SetUtcNow(DateTimeOffset value)
public void Advance(TimeSpan delta)
public void SetLocalTimeZone(TimeZoneInfo localTimeZone)
```

These can be used as follows:

```csharp
var timeProvider = new FakeTimeProvider();
var myComponent = new MyComponent(timeProvider);
timeProvider.Advance(TimeSpan.FromSeconds(5));
myComponent.CheckState();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
