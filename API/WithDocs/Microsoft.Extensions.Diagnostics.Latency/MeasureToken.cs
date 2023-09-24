// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Represents a registered measure.
/// </summary>
public readonly struct MeasureToken
{
    /// <summary>
    /// Gets the name of the measure.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the position of the token in the token table.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Latency.MeasureToken" /> struct.
    /// </summary>
    /// <param name="name">Name of the measure.</param>
    /// <param name="position">Position of the token in the token table.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    public MeasureToken(string name, int position);
}
