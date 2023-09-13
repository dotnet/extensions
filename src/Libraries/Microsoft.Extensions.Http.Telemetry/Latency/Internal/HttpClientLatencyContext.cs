// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Internal;

internal sealed class HttpClientLatencyContext
{
    private readonly AsyncLocal<ILatencyContext?> _latencyContext = new();

    public ILatencyContext? Get()
    {
        return _latencyContext.Value;
    }

    public void Set(ILatencyContext context)
    {
        _latencyContext.Value = context;
    }

    public void Unset()
    {
        _latencyContext.Value = null;
    }
}
