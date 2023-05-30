// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    public Type? ProviderType { get; }
    public string? ProviderMethod { get; }
    public bool SkipNullProperties { get; set; }
    public bool OmitParameterName { get; set; }
    public LogPropertiesAttribute();
    public LogPropertiesAttribute(Type providerType, string providerMethod);
}
