// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// No-op implementation of a latency context.
/// </summary>
internal sealed class NullLatencyContext : ILatencyContext, ILatencyContextProvider, ILatencyContextTokenIssuer
{
    private readonly ArraySegment<Checkpoint> _checkpoints = new(Array.Empty<Checkpoint>());
    private readonly ArraySegment<Tag> _tags = new(Array.Empty<Tag>());
    private readonly ArraySegment<Measure> _measures = new(Array.Empty<Measure>());

    public LatencyData LatencyData => new(_tags, _checkpoints, _measures, 0, TimeSpan.TicksPerSecond);

    public void Freeze()
    {
        // Nothing to do on Stop.
    }

    public ILatencyContext CreateContext() => this;

    public void Dispose()
    {
        // Method intentionally left empty.
    }

    public void SetTag(TagToken token, string value)
    {
        // Method intentionally left empty.
    }

    public void AddCheckpoint(CheckpointToken token)
    {
        // Method intentionally left empty.
    }

    public void AddMeasure(MeasureToken name, long value)
    {
        // Method intentionally left empty.
    }

    public void RecordMeasure(MeasureToken name, long value)
    {
        // Method intentionally left empty.
    }

    public TagToken GetTagToken(string name) => default;
    public CheckpointToken GetCheckpointToken(string name) => default;
    public MeasureToken GetMeasureToken(string name) => default;
}
