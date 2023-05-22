// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Extensions.Telemetry.Latency.Internal;
using Microsoft.Shared.Pools;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Latency.Test.Internal;

public class LatencyContextTest
{
    private readonly string[] _checkpoints = new[] { "ca", "cb", "lc", "cd" };
    private readonly string[] _tags = new[] { "ta", "tb", "tc", "td" };
    private readonly string[] _measures = new[] { "ma", "mb", "mc", "md" };

    [Fact]
    public void Context_Dispose_Stops_Context()
    {
        var latencyContext = GetContext();
        Assert.True(latencyContext.IsRunning);
        latencyContext.Dispose();
        Assert.False(latencyContext.IsRunning);
    }

    [Fact]
    public void Context_Dispose_InvokedMulitpleTimes()
    {
        var latencyContext = GetContext();
        Assert.NotNull(latencyContext);
        Assert.False(latencyContext.IsDisposed);
        latencyContext.Dispose();
        Assert.True(latencyContext.IsDisposed);
#pragma warning disable S3966 // Objects should not be disposed more than once
        latencyContext.Dispose();
        latencyContext.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once
    }

    [Fact]
    public void Context_StopOnlyOnce()
    {
        using var latencyContext = GetContext();
        latencyContext.Freeze();
        Assert.False(latencyContext.IsRunning);

        // Subsequent adds become no-ops
        latencyContext.Freeze();
        latencyContext.Freeze();
        Assert.False(latencyContext.IsRunning);
    }

    [Fact]
    public void Context_Dispose_ReturnsToPool()
    {
        var r = GetRegistry();
        var li = new LatencyInstrumentProvider(r);
        var lcp = new LatencyContextPool(li);
        var pool = new MockResetOnGet(lcp);
        lcp.Pool = pool;
        var latencyContext = new LatencyContext(lcp);
        Assert.False(latencyContext.IsDisposed);
        latencyContext.Dispose();
        Assert.True(latencyContext.IsDisposed);
        Assert.True(pool.ReturnCalled);
    }

    [Fact]
    public void Latency_Context_Is_Not_Adding_Measures_When_Frozen()
    {
        const string TokenName = nameof(TokenName);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var measureToken = tokenIssuer.GetMeasureToken(TokenName);

        context.Freeze();
        context.AddMeasure(measureToken, 1);
        context.RecordMeasure(measureToken, 1);

        var measures = context.LatencyData.Measures;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.False(((LatencyContext)context).IsRunning);
        Assert.Empty(measures.ToArray());
    }

    [Fact]
    public void Latency_Context_Is_Adding_Measures_When_Not_Frozen()
    {
        const string TokenName = nameof(TokenName);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .RegisterMeasureNames(TokenName)
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var measureToken = tokenIssuer.GetMeasureToken(TokenName);

        context.AddMeasure(measureToken, 1);

        var measures = context.LatencyData.Measures;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.True(((LatencyContext)context).IsRunning);
        Assert.Single(measures.ToArray());
        Assert.Equal(TokenName, measures[0].Name);
    }

    [Fact]
    public void Latency_Context_Is_Recording_Measures_When_Not_Frozen()
    {
        const string TokenName = nameof(TokenName);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .RegisterMeasureNames(TokenName)
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var measureToken = tokenIssuer.GetMeasureToken(TokenName);

        context.RecordMeasure(measureToken, 1);

        var measures = context.LatencyData.Measures;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.True(((LatencyContext)context).IsRunning);
        Assert.Single(measures.ToArray());
        Assert.Equal(TokenName, measures[0].Name);
    }

    [Fact]
    public void Latency_Context_Is_Not_Adding_Values_To_Tags_When_Frozen()
    {
        const string TokenName = nameof(TokenName);
        const string Tag = nameof(Tag);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .RegisterTagNames(TokenName)
            .RegisterCheckpointNames(TokenName)
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var tagToken = tokenIssuer.GetTagToken(TokenName);

        var tags2 = context.LatencyData.Tags;

        context.Freeze();
        context.SetTag(tagToken, Tag);

        var tags = context.LatencyData.Tags.ToArray();

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.False(((LatencyContext)context).IsRunning);
        Assert.Single(tags);
        Assert.Empty(tags[0].Value);
    }

    [Fact]
    public void Latency_Context_Is_Adding_Values_To_Tags_Tags_When_Not_Frozen()
    {
        const string TokenName = nameof(TokenName);
        const string Tag = nameof(Tag);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .RegisterTagNames(TokenName)
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var tagToken = tokenIssuer.GetTagToken(TokenName);

        context.SetTag(tagToken, Tag);

        var tags = context.LatencyData.Tags;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.True(((LatencyContext)context).IsRunning);
        Assert.Single(tags.ToArray());
        Assert.Equal(Tag, tags[0].Value);
    }

    [Fact]
    public async Task Latency_Context_Is_Returning_Const_Duration_When_Frozen()
    {
        const string TokenName = nameof(TokenName);
        const string Tag = nameof(Tag);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();

        context.Freeze();
        var afterFreezeDuration = context.LatencyData.DurationTimestamp;

        await Task.Delay(1);

        var afterDelayDuration = context.LatencyData.DurationTimestamp;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.False(((LatencyContext)context).IsRunning);
        Assert.True(afterFreezeDuration.Equals(afterDelayDuration));
    }

    [Fact]
    public void Latency_Context_Is_Not_Adding_Checkpoints_When_Frozen()
    {
        const string TokenName = nameof(TokenName);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var checkpointToken = tokenIssuer.GetCheckpointToken(TokenName);

        context.Freeze();
        context.AddCheckpoint(checkpointToken);

        var checkpoints = context.LatencyData.Checkpoints;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.False(((LatencyContext)context).IsRunning);
        Assert.Empty(checkpoints.ToArray());
    }

    [Fact]
    public void Latency_Context_Is_Adding_Checkpoints_When_Not_Frozen()
    {
        const string TokenName = nameof(TokenName);

        using var scope = new ServiceCollection()
            .AddLatencyContext()
            .RegisterCheckpointNames(TokenName)
            .BuildServiceProvider()
            .CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ILatencyContextProvider>().CreateContext();
        var tokenIssuer = services.GetRequiredService<ILatencyContextTokenIssuer>();
        var checkpointToken = tokenIssuer.GetCheckpointToken(TokenName);

        context.AddCheckpoint(checkpointToken);

        var checkpoints = context.LatencyData.Checkpoints;

        Assert.IsAssignableFrom<LatencyContext>(context);
        Assert.True(((LatencyContext)context).IsRunning);
        Assert.Single(checkpoints.ToArray());
        Assert.Equal(TokenName, checkpoints[0].Name);
    }

    private LatencyContext GetContext()
    {
        var r = GetRegistry();
        var li = new LatencyInstrumentProvider(r);
        var lcp = new LatencyContextPool(li);
        return lcp.Pool.Get();
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

    private class MockResetOnGet : ObjectPool<LatencyContext>
    {
        public bool ReturnCalled;
        private readonly ObjectPool<LatencyContext> _objectPool;

        public MockResetOnGet(LatencyContextPool latencyContextPool)
        {
            var policy = new LatencyContextPool.LatencyContextPolicy(latencyContextPool);
            _objectPool = PoolFactory.CreatePool(policy);
        }

        public override LatencyContext Get()
        {
            var o = _objectPool.Get();
            _ = o.TryReset();
            return o;
        }

        public override void Return(LatencyContext obj)
        {
            ReturnCalled = true;
            _objectPool.Return(obj);
        }
    }
}
