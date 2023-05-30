// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Name and value pair to provide metadata about a operation being measured.
/// </summary>
public readonly struct Tag
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the tag.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Latency.Tag" /> struct.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <param name="value">Value of the tag.</param>
    public Tag(string name, string value);
}
