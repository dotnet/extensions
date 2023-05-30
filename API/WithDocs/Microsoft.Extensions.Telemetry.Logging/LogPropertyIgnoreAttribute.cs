// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Indicates that a property should not be logged.
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" />.
[AttributeUsage(AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertyIgnoreAttribute : Attribute
{
    public LogPropertyIgnoreAttribute();
}
