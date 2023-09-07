// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Represents a registered checkpoint.
/// </summary>
public readonly struct CheckpointToken
{
    /// <summary>
    /// Gets the name of the checkpoint.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the position of token in the token table.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Latency.CheckpointToken" /> struct.
    /// </summary>
    /// <param name="name">Name of the checkpoint.</param>
    /// <param name="position">Position of the token in the token table.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public CheckpointToken(string name, int position);
}
