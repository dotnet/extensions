// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

/// <summary>
/// Implementation of <see cref="ILatencyContextProvider"/>.
/// </summary>
internal sealed class LatencyContextProvider : ILatencyContextProvider
{
    private readonly LatencyContextPool _latencyInstrumentPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyContextProvider"/> class.
    /// </summary>
    /// <param name="latencyInstrumentProvider">Latency instrument provider.</param>
    public LatencyContextProvider(LatencyInstrumentProvider latencyInstrumentProvider)
    {
        _latencyInstrumentPool = new LatencyContextPool(latencyInstrumentProvider);
    }

    public ILatencyContext CreateContext() => _latencyInstrumentPool.Pool.Get();
}
