// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metering;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DimensionAttribute : Attribute
{
    public string Name { get; }
    public DimensionAttribute(string name);
}
