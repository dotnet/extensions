// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public sealed class RedisFixture : IDisposable
{
    private ConnectionMultiplexer? _muxer;
    private Task<IConnectionMultiplexer?>? _sharedConnect;
    public Task<IConnectionMultiplexer?> ConnectAsync() => _sharedConnect ??= DoConnectAsync();

    public void Dispose() => _muxer?.Dispose();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "catch-all")]
    private async Task<IConnectionMultiplexer?> DoConnectAsync()
    {
        try
        {
            _muxer = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:6379");
            await _muxer.GetDatabase().PingAsync();
            return _muxer;
        }
        catch
        {
            return null;
        }
    }
}
