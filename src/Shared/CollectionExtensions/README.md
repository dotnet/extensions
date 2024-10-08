# Collection Extensions

`TryGetTypedValue` performs a ``TryGetValue` on a dictionary and then attempts to cast the value to the specified type. If the value is not of the specified type, false is returned.

To use this in your project, add the following to your `.csproj` file:

```xml
<PropertyGroup>
  <InjectSharedCollectionExtensions>true</InjectSharedCollectionExtensions>
</PropertyGroup>
```
