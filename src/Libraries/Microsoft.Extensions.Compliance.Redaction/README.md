# Microsoft.Extensions.Compliance.Redaction

A redaction engine and canonical redactors.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Compliance.Redaction
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Compliance.Redaction" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Registering the services

The services can be registered using one of the `AddRedaction` overloads:

```csharp
public static IServiceCollection AddRedaction(this IServiceCollection services);
public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure);
```

### Configuring a redactor

Redactors can be configured using one of these `IRedactionBuilder` extension methods:

```csharp
// Sets the redactor to use for a set of data classifications.
IRedactionBuilder SetRedactor<T>(params DataClassificationSet[] classifications);

/// Sets the redactor to use when processing classified data for which no specific redactor has been registered.
IRedactionBuilder SetFallbackRedactor<T>();
```

The `ErasingRedactor` is the default fallback redactor. If no redactor is configured for a data classification then the data will be erased.

### Configuring the HMAC redactor

The HMAC redactor can be configured using one these `IRedactionBuilder` extension methods:

```csharp
public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, Action<HmacRedactorOptions> configure, params DataClassificationSet[] classifications);

public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassificationSet[] classifications);
```

The `HmacRedactorOptions` requires its `KeyId` and `Key` properties to be set.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
