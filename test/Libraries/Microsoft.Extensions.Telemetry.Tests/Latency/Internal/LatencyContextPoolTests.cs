// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class LatencyContextPoolTests
{
    private readonly string[] _checkpoints = new[] { "ca", "cb", "cc", "cd" };
    private readonly string[] _tags = new[] { "ta", "tb", "tc", "td" };
    private readonly string[] _measures = new[] { "ma", "mb", "mc", "md" };

    [Fact]
    public void LatencyContextPool_GivesInstruments()
    {
        var lcp = GetLatencyContextPool();
        Assert.NotNull(lcp);
        using var lc = lcp.Pool.Get();
        Assert.NotNull(lc);
    }

    [Fact]
    public void LatencyContextPool_DoesNotGive_ContextInUse()
    {
        var lcp = GetLatencyContextPool();

        using var lc = lcp.Pool.Get();
        using var lc1 = lcp.Pool.Get();
        using var lc2 = lcp.Pool.Get();

        Assert.NotEqual(lc, lc1);
        Assert.NotEqual(lc, lc2);
        Assert.NotEqual(lc1, lc2);
    }

    [Fact]
    public void LatencyContextPool_Get_LatencyContextCorrectState()
    {
        var lcp = GetLatencyContextPool();

        var o = lcp.Pool.Get();
        Assert.True(o.IsRunning);
        Assert.False(o.IsDisposed);
        o.Dispose();
        Assert.False(o.IsRunning);
        Assert.True(o.IsDisposed);
        var o1 = lcp.Pool.Get();
        Assert.True(o1.IsRunning);
        Assert.False(o.IsDisposed);
    }

    [Fact]
    public void RestOnGetPool_Get_CallsReset()
    {
        var p = new ResetOnGetObjectPool<Resettable>(
            new NoResetPolicy());

        var o = p.Get();
        Assert.True(o.ResetCalled == 1);
        p.Return(o);
        o = p.Get();
        Assert.True(o.ResetCalled == 1);
    }

    private class NoResetPolicy : PooledObjectPolicy<Resettable>
    {
        public override Resettable Create()
        {
            return new Resettable();
        }

        public override bool Return(Resettable obj) => false;
    }

    private class Resettable : IResettable
    {
        public int ResetCalled;

        public bool TryReset()
        {
            ResetCalled++;
            return true;
        }
    }

    private LatencyContextPool GetLatencyContextPool()
    {
        return new LatencyContextPool(new LatencyInstrumentProvider(GetRegistry()));
    }

    private LatencyContextRegistrySet GetRegistry()
    {
        var option = MockLatencyContextRegistrationOptions.GetLatencyContextRegistrationOptions(
            _checkpoints, _measures, _tags);

        var lco = new Mock<IOptions<LatencyContextOptions>>();
        lco.Setup(a => a.Value).Returns(new LatencyContextOptions { ThrowOnUnregisteredNames = false });

        var r = new LatencyContextRegistrySet(lco.Object, option);

        return r;
    }
}
