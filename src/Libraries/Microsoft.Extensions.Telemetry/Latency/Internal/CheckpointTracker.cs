// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

/// <summary>
/// Class that tracks checkpoints.
/// </summary>
internal sealed class CheckpointTracker : IResettable
{
    internal TimeProvider TimeProvider;
    private readonly Registry _checkpointNames;
    private readonly int[] _checkpointAdded;
    private readonly Checkpoint[] _checkpoints;

    private long _timestamp;

    private int _numCheckpoints;

    public long Elapsed => TimeProvider.GetTimestamp() - _timestamp;

    public long Frequency => TimeProvider.TimestampFrequency;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckpointTracker"/> class.
    /// </summary>
    /// <param name="registry">Registry of checkpoint names.</param>
    public CheckpointTracker(Registry registry)
    {
        _checkpointNames = registry;
        var keyCount = _checkpointNames.KeyCount;
        _checkpointAdded = new int[keyCount];
        _checkpoints = new Checkpoint[keyCount];
        TimeProvider = TimeProvider.System;
        _timestamp = TimeProvider.GetTimestamp();
    }

    /// <summary>
    /// Resets the CheckpointTracker.
    /// </summary>
    public bool TryReset()
    {
        _timestamp = TimeProvider.GetTimestamp();
        _numCheckpoints = 0;
        Array.Clear(_checkpointAdded, 0, _checkpointAdded.Length);
        return true;
    }

    public CheckpointToken GetToken(string name)
    {
        int pos = _checkpointNames.GetRegisteredKeyIndex(name);
        return new CheckpointToken(name, pos);
    }

    /// <summary>
    /// Add checkpoint for token.
    /// </summary>
    /// <param name="token">Token for checkpoint.</param>
    /// <remarks> If same checkpoint is added more than once, first write wins.</remarks>
    public void Add(CheckpointToken token)
    {
        if (token.Position > -1 && Interlocked.CompareExchange(ref _checkpointAdded[token.Position], 1, 0) == 0)
        {
            var p = Interlocked.Increment(ref _numCheckpoints);
            _checkpoints[p - 1] = new Checkpoint(token.Name, Elapsed, Frequency);
        }
    }

    /// <summary>
    /// Gets list of checkpoints added.
    /// </summary>
    public ArraySegment<Checkpoint> Checkpoints => new(_checkpoints, 0, _numCheckpoints);
}
