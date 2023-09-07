// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Represents a registered tag.
/// </summary>
public readonly struct TagToken
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the position of the token in the token table.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Latency.TagToken" /> struct.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <param name="position">Position of the token in the token table.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public TagToken(string name, int position);
}
