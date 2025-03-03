# Microsoft.Extensions.Compliance.Abstractions

This package introduces data classification and data redaction features.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.Compliance.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Compliance.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Data Classification

The `DataClassification` structure encapsulates a classification label within a specific taxonomy for your data. It allows you to mark sensitive information and enforce policies based on classifications.

- **Taxonomy Name:** Identifies the classification system.
- **Value:** Represents the specific label within the taxonomy.

#### Creating Custom Classifications

You can define custom classifications by creating static members that represent different types of sensitive data. This provides a consistent way to label and handle data across your application.

Example:
```csharp
using Microsoft.Extensions.Compliance.Classification;

public static class MyTaxonomyClassifications
{
    public static string Name => "MyTaxonomy";

    public static DataClassification PrivateInformation => new DataClassification(Name, nameof(PrivateInformation));
    public static DataClassification CreditCardNumber => new DataClassification(Name, nameof(CreditCardNumber));
    public static DataClassification SocialSecurityNumber => new DataClassification(Name, nameof(SocialSecurityNumber));
}
```

#### Binding Data Classification Settings

You can bind data classification settings directly from your configuration using the options pattern. For example:

appsettings.json
```json
{
    "Key": {
        "PhoneNumber": "MyTaxonomy:PrivateInformation",
        "ExampleDictionary": {
            "CreditCard": "MyTaxonomy:CreditCardNumber",
            "SSN": "MyTaxonomy:SocialSecurityNumber",
        }
    }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class TestOptions
{
    public DataClassification? PhoneNumber { get; set; }
    public IDictionary<string, DataClassification> ExampleDictionary { get; set; } = new Dictionary<string, DataClassification>();
}

class Program
{
    static void Main(string[] args)
    {
        // Build configuration from an external json file.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup DI container and bind the configuration section "Key" to TestOptions.
        IServiceCollection services = new ServiceCollection();
        services.Configure<TestOptions>(configuration.GetSection("Key"));

        // Build the service provider.
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Get the bound options.
        TestOptions options = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;

        // Simple output demonstrating binding results.
        Console.WriteLine("Configuration bound to TestOptions:");
        Console.WriteLine($"PhoneNumber: {options.PhoneNumber}");
        foreach (var item in options.ExampleDictionary)
        {
            Console.WriteLine($"{item.Key}: {item.Value}");
        }
    }
}
```

### Implementing Redactors

Redactors can be implemented by inheriting from `Microsoft.Extensions.Compliance.Redaction.Redactor`. For example:

```csharp
using Microsoft.Extensions.Compliance.Redaction;

public class StarRedactor : Redactor
{
    private const string Stars = "****";

    public override int GetRedactedLength(ReadOnlySpan<char> input) => Stars.Length;

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        Stars.CopyTo(destination);
        return Stars.Length;
    }
}
```

### Implementing Redactor Providers

Redactor Providers implement `Microsoft.Extensions.Compliance.Redaction.IRedactorProvider`.
For example:

```csharp
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

public sealed class StarRedactorProvider : IRedactorProvider
{
    private static readonly StarRedactor _starRedactor = new();

    public static StarRedactorProvider Instance { get; } = new();

    public Redactor GetRedactor(DataClassificationSet classifications) => _starRedactor;
}
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
