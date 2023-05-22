// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency.Internal;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Latency.Test.Internal;

public class LatencyContextRegistrySetTest
{
    [Fact]
    public void ServiceCollection_Register_DefaultOption()
    {
        var lco = new Mock<IOptions<LatencyContextOptions>>();
        lco.Setup(a => a.Value).Returns(new LatencyContextOptions());

        var lcrs = new LatencyContextRegistrySet(lco.Object);
        Assert.NotNull(lcrs);
        Assert.NotNull(lcrs.CheckpointNameRegistry);
        Assert.NotNull(lcrs.TagNameRegistry);
        Assert.NotNull(lcrs.MeasureNameRegistry);
        Assert.True(lcrs.CheckpointNameRegistry.KeyCount == 0);
        Assert.True(lcrs.MeasureNameRegistry.KeyCount == 0);
        Assert.True(lcrs.TagNameRegistry.KeyCount == 0);
    }

    [Fact]
    public void Registry_Add_BasicTest()
    {
        var s = new[] { "a", "b", "c", "d" };
        var r = GetRegistry(s, s, s);

        CheckRegistration(r.CheckpointNameRegistry, "c", "e");
        CheckRegistration(r.MeasureNameRegistry, "d", "e");
        CheckRegistration(r.TagNameRegistry, "a", "e");
    }

    [Fact]
    public void ServiceCollection_Register_InvalidValues()
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        string[] n = new[] { "a", "b", null, "d" };
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        var e = Array.Empty<string>();

        Assert.Throws<ArgumentException>(() => GetRegistry(n, e, e));
        Assert.Throws<ArgumentException>(() => GetRegistry(e, n, e));
        Assert.Throws<ArgumentException>(() => GetRegistry(e, e, n));

        n = new[] { "  ", "b", "c" };
        Assert.Throws<ArgumentException>(() => GetRegistry(n, e, e));
        Assert.Throws<ArgumentException>(() => GetRegistry(e, n, e));
        Assert.Throws<ArgumentException>(() => GetRegistry(e, e, n));
    }

    [Fact]
    public void ServiceCollection_Register_AddsToRegistry()
    {
        var checkpoints = new[] { "ca", "cb" };
        var measures = new[] { "ma", "mb" };
        var tags = new[] { "ta", "tb" };

        var lcr = GetRegistry(checkpoints, measures, tags);

        Assert.True(lcr.CheckpointNameRegistry.IsRegistered("ca"));
        Assert.True(lcr.CheckpointNameRegistry.KeyCount == 2);
        Assert.True(lcr.MeasureNameRegistry.IsRegistered("ma"));
        Assert.True(lcr.MeasureNameRegistry.KeyCount == 2);
        Assert.True(lcr.TagNameRegistry.IsRegistered("ta"));
        Assert.True(lcr.TagNameRegistry.KeyCount == 2);
    }

    private static void CheckRegistration(Registry registry, string registered, string notRegsitered)
    {
        Assert.True(registry.IsRegistered(registered));
        Assert.False(registry.IsRegistered(notRegsitered));
    }

    private static LatencyContextRegistrySet GetRegistry(string[] checkpoints, string[] measures, string[] tags)
    {
        var lco = new Mock<IOptions<LatencyContextOptions>>();
        lco.Setup(a => a.Value).Returns(new LatencyContextOptions());

        var o = MockLatencyContextRegistrationOptions.GetLatencyContextRegistrationOptions(checkpoints, measures, tags);
        var r = new LatencyContextRegistrySet(lco.Object, o);
        return r;
    }
}
