// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Marks a logging method parameter whose public tags need to be logged.
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.LoggerMessageAttribute" />
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether <see langword="null" /> tags are logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />.
    /// </value>
    public bool SkipNullProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefix the name of the parameter or property to the generated name of each tag being logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />.
    /// </value>
    public bool OmitReferenceName { get; set; }

    public LogPropertiesAttribute();
}
