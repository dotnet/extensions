# Microsoft.Extensions.AuditReports

Produces reports about the code being compiled which are useful during privacy and telemetry audits.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AuditReports
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AuditReports" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Available reports

The following reports are available in this package:

- **Metrics**: Reports on the use of source-generated metric definitions in the code.
- **Compliance**: Reports on the use of privacy-sensitive data in the code, including source-generated logging methods.

The table below shows various MSBuild properties that you can use to control the behavior of the reports generation:

| Metrics report generator | Compliance report generator | Description |
| --- | --- | --- |
| `<GenerateMetricsReport>` | `<GenerateComplianceReport>` | Controls whether the report is generated. |
| `<MetricsReportOutputPath>` | `<ComplianceReportOutputPath>` | The path to the directory where the report will be generated. |

The file names of the reports are defined by the corresponding report generator.
The metrics report will be generated in a file named `MetricsReport.json`.
The compliance report will be generated in a file named `ComplianceReport.json`.

For example, to generate a compliance report, you can add the following to your project file:

```xml
<PropertyGroup>
  <GenerateComplianceReport>true</GenerateComplianceReport>
  <ComplianceReportOutputPath>C:\AuditReports</ComplianceReportOutputPath>
</PropertyGroup>
```

Both report generators follow the same strategy if you don't provide a value for the property with the output path (`ComplianceReportOutputPath` or `MetricsReportOutputPath`).
In that case, the report path will be determined via the following strategy:

1. If the `$(OutputPath)` property is defined and it's an absolute path, the report will be generated in that directory.
2. If both `$(OutputPath)` and `$(ProjectDir)` properties are defined and the `$(OutputPath)` property contains a relative path, the report will be generated in the `$(ProjectDir)\$(OutputPath)` directory.

If none of the above conditions are met, the report will not be generated and the diagnostic message will be emitted.

## Example of a compliance report

Let's assume we have a project with a class that contains privacy-sensitive data:

```csharp
namespace ComplianceTesting
{
    internal sealed class User
    {
        internal User(string name, DateTimeOffset registeredAt)
        {
            Name = name;
            RegisteredAt = registeredAt;
        }

        [PrivateData]
        public string Name { get; }

        public DateTimeOffset RegisteredAt { get; }
    }
}
```

`Microsoft.Extensions.Compliance.Testing` package contains a definition for `[PrivateData]` attribute, we use it here for demonstration purposes only.

A compliance report for the code listed above might look like this:

```json
{
    "Name": "MyAssembly",
    "Types": [
        {
            "Name": "ComplianceTesting.User",
            "Members": [
                {
                    "Name": "Name",
                    "Type": "string",
                    "File": "C:\\source\\samples\\src\\MyAssembly\\User.cs",
                    "Line": "12",
                    "Classifications": [
                        {
                            "Name": "PrivateData"
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Example of a metrics report

Let's assume we have a project with a class that contains a source-generated metric definition:

```csharp
internal sealed partial class Metric
{
    internal static class Tags
    {
        /// <summary>
        /// The target of the metric, e.g. the name of the service or the name of the method.
        /// </summary>
        public const string Target = nameof(Target);

        /// <summary>
        /// The reason for the failure, e.g. the exception message or the HTTP status code.
        /// </summary>
        public const string FailureReason = nameof(FailureReason);
    }

    /// <summary>
    /// The counter metric for the number of failed requests.
    /// </summary>
    [Counter(Tags.Target, Tags.FailureReason)]
    public static partial FailedRequestCounter CreateFailedRequestCounter(Meter meter);
}
```

A metrics report for the code listed above might look like this:

```json
[
 {
  "MyAssembly":
  [
    {
     "MetricName": "FailedRequestCounter",
     "MetricDescription": "The counter metric for the number of failed requests.",
     "InstrumentName": "Counter",
     "Dimensions": {
      "Target": "The target of the metric, e.g. the name of the service or the name of the method.",
      "FailureReason": "The reason for the failure, e.g. the exception message or the HTTP status code."
      }
    }
  ]
 }
]
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
