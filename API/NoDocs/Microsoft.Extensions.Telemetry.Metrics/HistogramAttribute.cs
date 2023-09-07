// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metrics;

[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class HistogramAttribute : Attribute
{
    public string? Name { get; set; }
    public string[]? TagNames { get; }
    public Type? Type { get; }
    public HistogramAttribute(params string[] tagNames);
    public HistogramAttribute(Type type);
}
