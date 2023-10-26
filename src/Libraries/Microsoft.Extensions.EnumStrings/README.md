# Microsoft.Extensions.EnumStrings

It's often necessary to convert a C# enum value to a string. The standard ToString() is normally used for this, but that can not be the best in terms of allocations or performance. This package introduces a source generator that is triggered by an attribute which produces an extension method for each enum type annotated with the attribute which can efficiently convert an enum value to a string.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.EnumStrings
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.EnumStrings" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Adding the attribute to an enum

To use this feature, you can annotate an enum value with the `EnumStringAttribute` attribute. For example:

```csharp
using Microsoft.Extensions.EnumStrings;

[EnumStrings]
public enum Color
{
    Red,
    Green,
    Blue
}
```

As a result, the code generator produces a method with the following signature:

```csharp
internal static class ColorExtensions
{
    public static string ToInvariantString(this Color value);
}
```

Which you can later use as follows:

```csharp
{
    var color = Color.Red;

    var s1 = color.ToString(); // <- This line will cause an allocation and won't cache the result.
    var s2 = color.ToInvariantString(); // <- This call is more performant and won't cause a new allocation.
}
```

Both calls will return the same string, but the second one will be more efficient.

### Using the generated with an enum from a different assembly

You can also use this feature to generate efficient `ToInvariantString` extension methods on enums that are not part of your code. You can do this by applying the `EnumStrings` attribute at the assembly level, and passing in the type of the enum you want to generate the extension method for. For example:

```csharp
[assembly: EnumStrings(typeof(Color))]
```

### Controlling the generated code

The `EnumStringsAttribute` has a few optional properties to let you control the generated class and method. Here are the things that can be controlled:

- `ExtensionNamespace`: Lets you control the namespace into which the extension class is generated. By default, the class will be generated in the same namespace as the enum.
- `ExtensionClassName`: Lets you control the name of the extension class. This defaults to the name of the enum with the `Extensions` suffix appended to it. For example, if the enum is named `Color`, the extension class will be named `ColorExtensions`.
- `ExtensionMethodName`: Lets you control the name of the method that is generated. This defaults to `ToInvariantString`.
- `ExtensionClassModifiers`: Lets you control the access modifiers of the generated class. This defaults to `internal static`. Another common value would be `public static`.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
