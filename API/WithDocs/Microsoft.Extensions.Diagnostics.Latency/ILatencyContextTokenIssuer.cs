// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Issues tokens for various object types.
/// </summary>
public interface ILatencyContextTokenIssuer
{
    /// <summary>
    /// Gets a token for a named tag.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <returns>Token to use with <see cref="M:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext.SetTag(Microsoft.Extensions.Diagnostics.Latency.TagToken,System.String)" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    TagToken GetTagToken(string name);

    /// <summary>
    /// Gets a token for a named checkpoint.
    /// </summary>
    /// <param name="name">Name of the checkpoint.</param>
    /// <returns>Token to use with <see cref="M:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext.AddCheckpoint(Microsoft.Extensions.Diagnostics.Latency.CheckpointToken)" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    CheckpointToken GetCheckpointToken(string name);

    /// <summary>
    /// Gets a token for a named measure.
    /// </summary>
    /// <param name="name">Name of the measure.</param>
    /// <returns>Token to use with <see cref="M:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext.AddMeasure(Microsoft.Extensions.Diagnostics.Latency.MeasureToken,System.Int64)" />
    /// and <see cref="M:Microsoft.Extensions.Diagnostics.Latency.ILatencyContext.RecordMeasure(Microsoft.Extensions.Diagnostics.Latency.MeasureToken,System.Int64)" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    MeasureToken GetMeasureToken(string name);
}
