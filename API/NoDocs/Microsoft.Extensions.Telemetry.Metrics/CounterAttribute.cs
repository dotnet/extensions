// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metrics;

[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class CounterAttribute : Attribute
{
    public string? Name { get; set; }
    public string[]? TagNames { get; }
    public Type? Type { get; }
    public CounterAttribute(params string[] tagNames);
    public CounterAttribute(Type type);
}
