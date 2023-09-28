// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

internal sealed class LatencyInstrumentProvider
{
    private readonly LatencyContextRegistrySet _latencyContextRegistrySet;

    public LatencyInstrumentProvider(LatencyContextRegistrySet latencyContextRegistrySet)
    {
        _latencyContextRegistrySet = latencyContextRegistrySet;
    }

    public CheckpointTracker CreateCheckpointTracker()
    {
        return new CheckpointTracker(_latencyContextRegistrySet.CheckpointNameRegistry);
    }

    public MeasureTracker CreateMeasureTracker()
    {
        return new MeasureTracker(_latencyContextRegistrySet.MeasureNameRegistry);
    }

    public TagCollection CreateTagCollection()
    {
        return new TagCollection(_latencyContextRegistrySet.TagNameRegistry);
    }
}
