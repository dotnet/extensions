// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Metrics;

[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class GaugeAttribute : Attribute
{
    public string? Name { get; set; }
    public string[]? TagNames { get; }
    public Type? Type { get; }
    public GaugeAttribute(params string[] tagNames);
    public GaugeAttribute(Type type);
}
