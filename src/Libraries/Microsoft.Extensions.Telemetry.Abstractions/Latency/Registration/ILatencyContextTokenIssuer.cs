// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Issues tokens for various object types.
/// </summary>
public interface ILatencyContextTokenIssuer
{
    /// <summary>
    /// Gets a token for a named tag.
    /// </summary>
    /// <param name="name">Name of the tag.</param>
    /// <returns>Token to use with <see cref="ILatencyContext.SetTag(TagToken, string)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    TagToken GetTagToken(string name);

    /// <summary>
    /// Gets a token for a named checkpoint.
    /// </summary>
    /// <param name="name">Name of the checkpoint.</param>
    /// <returns>Token to use with <see cref="ILatencyContext.AddCheckpoint(CheckpointToken)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    CheckpointToken GetCheckpointToken(string name);

    /// <summary>
    /// Gets a token for a named measure.
    /// </summary>
    /// <param name="name">Name of the measure.</param>
    /// <returns>Token to use with <see cref="ILatencyContext.AddMeasure(MeasureToken, long)"/>
    /// and <see cref="ILatencyContext.RecordMeasure(MeasureToken, long)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    MeasureToken GetMeasureToken(string name);
}
