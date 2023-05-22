// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Latency;

namespace Microsoft.Extensions.Telemetry.Latency.Internal;

/// <summary>
/// Implementation of <see cref="ILatencyContext"/>.
/// </summary>
internal sealed class LatencyContext : ILatencyContext, IResettable
{
    internal bool IsDisposed;

    internal bool IsRunning;

    private readonly ObjectPool<LatencyContext> _poolToReturnTo;

    private readonly CheckpointTracker _checkpointTracker;

    private readonly TagCollection _tagCollection;

    private readonly MeasureTracker _measureTracker;

    private long _duration;

    public LatencyContext(LatencyContextPool latencyContextPool)
    {
        var latencyInstrumentProvider = latencyContextPool.LatencyInstrumentProvider;
        _checkpointTracker = latencyInstrumentProvider.CreateCheckpointTracker();
        _measureTracker = latencyInstrumentProvider.CreateMeasureTracker();
        _tagCollection = latencyInstrumentProvider.CreateTagCollection();
        _poolToReturnTo = latencyContextPool.Pool;
        IsRunning = true;
    }

    public LatencyData LatencyData => IsDisposed ? default : new(_tagCollection.Tags, _checkpointTracker.Checkpoints, _measureTracker.Measures, Duration, _checkpointTracker.Frequency);

    private long Duration => IsRunning ? _checkpointTracker.Elapsed : _duration;

    #region Checkpoints
    public void AddCheckpoint(CheckpointToken token)
    {
        if (IsRunning)
        {
            _checkpointTracker.Add(token);
        }
    }
    #endregion

    #region Tags
    public void SetTag(TagToken token, string value)
    {
        if (IsRunning)
        {
            _tagCollection.Set(token, value);
        }
    }
    #endregion

    #region Measure
    public void AddMeasure(MeasureToken token, long value)
    {
        if (IsRunning)
        {
            _measureTracker.AddLong(token, value);
        }
    }

    public void RecordMeasure(MeasureToken token, long value)
    {
        if (IsRunning)
        {
            _measureTracker.SetLong(token, value);
        }
    }
    #endregion

    #region State
    public void Freeze()
    {
        if (IsRunning)
        {
            IsRunning = false;
            _duration = _checkpointTracker.Elapsed;
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (IsRunning)
        {
            Freeze();
        }

        _poolToReturnTo.Return(this);
        IsDisposed = true;
    }

    public bool TryReset()
    {
        _ = _checkpointTracker.TryReset();
        _ = _measureTracker.TryReset();
        _ = _tagCollection.TryReset();
        IsRunning = true;
        IsDisposed = false;
        return true;
    }
    #endregion
}
