// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Indicates that a tag should not be logged.
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.LoggerMessageAttribute" />.
[AttributeUsage(AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertyIgnoreAttribute : Attribute
{
    public LogPropertyIgnoreAttribute();
}
