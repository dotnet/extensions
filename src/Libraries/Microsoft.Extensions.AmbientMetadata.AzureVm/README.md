# Microsoft.Extensions.AmbientMetadata.AzureVm

This flows runtime information for Azure virtual machine instance ambient metadata such as the location, name, offer, etc. This information can be useful to enrich telemetry.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AmbientMetadata.AzureVm
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AmbientMetadata.AzureVm" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Configuration

The configuration can be load automatically from the Azure VM endpoint using the following method:

```csharp
public static IConfigurationBuilder AddAzureVmMetadata(this IConfigurationBuilder builder)
```

Alternatively, for purposes of unit testing or emulating Azure Virtual machine environment, all metadata values can be read from the `ambientmetadata:azurevm` section.

```json
{
  "AmbientMetadata": {
    "AzureVm": {
      "Location": "East US",
      "Offer": "Standard_DS1_v2"
      ...
    }
  }
}
```

### Registering Services

The services can be registered using any of the following methods:

```csharp
public static IHostBuilder UseAzureVmMetadata(this IHostBuilder builder, string sectionName = DefaultSectionName)
public static IServiceCollection AddAzureVmMetadata(this IServiceCollection services, Action<AzureVmMetadata> configure)
```

### Consuming Services

The `AzureVmMetadata` can be injected wherever needed. For example:

```csharp
public class MyClass
{
  public MyClass(IOptions<AzureVmMetadata> options) { Metadata = options.Value; }

  private AzureVmMetadata Metadata { get; }

  public void DoWork()
  {
    Log.LogEnvironment(Metadata.Location, Metadata.Offer);
  }
}
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
