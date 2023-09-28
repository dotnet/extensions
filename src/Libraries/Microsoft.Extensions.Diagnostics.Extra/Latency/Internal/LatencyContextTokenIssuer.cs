// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

internal sealed class LatencyContextTokenIssuer : ILatencyContextTokenIssuer
{
    private readonly CheckpointTracker _checkpointTracker;

    private readonly MeasureTracker _measureTracker;

    private readonly TagCollection _tagCollection;

    public LatencyContextTokenIssuer(LatencyInstrumentProvider latencyInstrumentProvider)
    {
        _checkpointTracker = latencyInstrumentProvider.CreateCheckpointTracker();
        _measureTracker = latencyInstrumentProvider.CreateMeasureTracker();
        _tagCollection = latencyInstrumentProvider.CreateTagCollection();
    }

    public CheckpointToken GetCheckpointToken(string name)
    {
        _ = Throw.IfNull(name);
        return _checkpointTracker.GetToken(name);
    }

    public TagToken GetTagToken(string name)
    {
        _ = Throw.IfNull(name);
        return _tagCollection.GetToken(name);
    }

    public MeasureToken GetMeasureToken(string name)
    {
        _ = Throw.IfNull(name);
        return _measureTracker.GetToken(name);
    }
}
