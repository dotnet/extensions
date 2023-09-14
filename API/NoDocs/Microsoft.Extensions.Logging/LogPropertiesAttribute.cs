// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    public bool SkipNullProperties { get; set; }
    public bool OmitReferenceName { get; set; }
    public LogPropertiesAttribute();
}
