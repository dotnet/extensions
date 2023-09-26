// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class LatencyContextProviderTests
{
    [Fact]
    public void Provider_CreateGetsNewContext()
    {
        var options = new LatencyContextOptions
        {
            ThrowOnUnregisteredNames = false
        };

        var lip = GetLatencyInstrumentProvider(options);
        var lcp = new LatencyContextProvider(lip);

        Assert.NotNull(lcp.CreateContext());
        Assert.NotSame(lcp.CreateContext(), lcp.CreateContext());
    }

    [Fact]
    public void Provider_NoThrowOptions()
    {
        var options = new LatencyContextOptions
        {
            ThrowOnUnregisteredNames = false
        };

        var lip = GetLatencyInstrumentProvider(options);
        var lcp = new LatencyContextProvider(lip);

        var tokenissuer = GetTokenIssuer(options);
        var ct = tokenissuer.GetCheckpointToken("ca");
        var mt = tokenissuer.GetMeasureToken("ma");
        var tt = tokenissuer.GetTagToken("ta");

        var lc = lcp.CreateContext();
        lc.AddCheckpoint(ct);
        lc.RecordMeasure(mt, 5);
        lc.AddMeasure(mt, 10);
        lc.SetTag(tt, "tag");

        ct = tokenissuer.GetCheckpointToken("ca1");
        mt = tokenissuer.GetMeasureToken("ma1");
        tt = tokenissuer.GetTagToken("ta1");

        lc.AddCheckpoint(ct);
        lc.RecordMeasure(mt, 5);
        lc.AddMeasure(mt, 10);
        lc.SetTag(tt, "tag");

        Assert.True(lc.LatencyData.Checkpoints.Length == 1);
        Assert.True(lc.LatencyData.Measures.Length == 1);
        Assert.True(lc.LatencyData.Tags.Length == 1);
    }

    [Fact]
    public void Provider_ThrowOptions()
    {
        LatencyContextOptions options = new LatencyContextOptions
        {
            ThrowOnUnregisteredNames = true
        };

        var lip = GetLatencyInstrumentProvider(options);
        var lcp = new LatencyContextProvider(lip);

        var tokenissuer = GetTokenIssuer(options);
        var ct = tokenissuer.GetCheckpointToken("ca");
        var mt = tokenissuer.GetMeasureToken("ma");
        var tt = tokenissuer.GetTagToken("ta");

        var lc = lcp.CreateContext();
        lc.AddCheckpoint(ct);
        lc.RecordMeasure(mt, 5);
        lc.AddMeasure(mt, 10);
        lc.SetTag(tt, "tag");

        Assert.Throws<ArgumentException>(() => tokenissuer.GetCheckpointToken("ca1"));
        Assert.Throws<ArgumentException>(() => tokenissuer.GetMeasureToken("ma1"));
        Assert.Throws<ArgumentException>(() => tokenissuer.GetTagToken("ta1"));

        Assert.True(lc.LatencyData.Checkpoints.Length == 1);
        Assert.True(lc.LatencyData.Measures.Length == 1);
        Assert.True(lc.LatencyData.Tags.Length == 1);
    }

    private static ILatencyContextTokenIssuer GetTokenIssuer(LatencyContextOptions options)
    {
        var lco = new Mock<IOptions<LatencyContextOptions>>();
        lco.Setup(a => a.Value).Returns(options);

        var lip = GetLatencyInstrumentProvider(options);

        return new LatencyContextTokenIssuer(lip);
    }

    private static LatencyInstrumentProvider GetLatencyInstrumentProvider(LatencyContextOptions options)
    {
        var lco = new Mock<IOptions<LatencyContextOptions>>();
        lco.Setup(a => a.Value).Returns(options);
        var lcr = new LatencyContextRegistrySet(lco.Object, GetRegistrationOption());

        return new LatencyInstrumentProvider(lcr);
    }

    private static IOptions<LatencyContextRegistrationOptions> GetRegistrationOption()
    {
        return MockLatencyContextRegistrationOptions.GetLatencyContextRegistrationOptions(
            new[] { "ca" }, new[] { "ma" }, new[] { "ta" });
    }
}
