# Microsoft.Extensions.Diagnostics.ResourceMonitoring

Measures and reports processor and memory usage. This library utilizes control groups (cgroups) in Linux to monitor system resources.

> [!NOTE]
> Currently, it supports cgroups v1 but does not have support for cgroups v2.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.Diagnostics.ResourceMonitoring
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Diagnostics.ResourceMonitoring" Version="[CURRENTVERSION]" />
</ItemGroup>
```


## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
