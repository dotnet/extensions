// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

/// <summary>
/// Object pools for instruments used for latency measurement.
/// </summary>
internal sealed class LatencyContextPool
{
    internal ObjectPool<LatencyContext> Pool { get; set; }

    internal readonly LatencyInstrumentProvider LatencyInstrumentProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyContextPool"/> class.
    /// </summary>
    public LatencyContextPool(LatencyInstrumentProvider latencyInstrumentProvider)
    {
        LatencyInstrumentProvider = latencyInstrumentProvider;
        var lcp = new LatencyContextPolicy(this);
        Pool = new ResetOnGetObjectPool<LatencyContext>(lcp);
    }

    /// <summary>
    /// Object pool policy for <see cref="LatencyContextPool"/>.
    /// </summary>
    internal sealed class LatencyContextPolicy : PooledObjectPolicy<LatencyContext>
    {
        private readonly LatencyContextPool _latencyContextPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyContextPolicy"/> class.
        /// </summary>
        public LatencyContextPolicy(LatencyContextPool latencyContextPool)
        {
            _latencyContextPool = latencyContextPool;
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <returns>Created object.</returns>
        public override LatencyContext Create()
        {
            return new LatencyContext(_latencyContextPool);
        }

        /// <summary>
        /// Return object to the pool.
        /// </summary>
        /// <param name="obj">Object to be returned.</param>
        /// <returns>True, indicating object is to be returned.</returns>
        public override bool Return(LatencyContext obj) => true;
    }
}
