# Microsoft.Extensions.Compliance.Abstractions

This package introduces data classification and data redaction features.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Compliance.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Compliance.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

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
