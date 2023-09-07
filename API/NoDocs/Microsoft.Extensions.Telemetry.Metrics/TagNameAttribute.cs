// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metrics;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TagNameAttribute : Attribute
{
    public string Name { get; }
    public TagNameAttribute(string name);
}
