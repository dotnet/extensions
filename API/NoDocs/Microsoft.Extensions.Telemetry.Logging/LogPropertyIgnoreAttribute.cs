// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Logging;

[AttributeUsage(AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertyIgnoreAttribute : Attribute
{
    public LogPropertyIgnoreAttribute();
}
