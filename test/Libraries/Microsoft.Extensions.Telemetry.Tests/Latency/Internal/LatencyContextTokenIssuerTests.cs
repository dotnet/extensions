// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class LatencyContextTokenIssuerTests
{
    private readonly string[] _checkpoints = new[] { "ca", "cb", "lc", "cd" };
    private readonly string[] _tags = new[] { "ta", "tb", "tc", "td" };
    private readonly string[] _measures = new[] { "ma", "mb", "mc", "md" };

    [Fact]
    public void TokenIssuer_ValidNames()
    {
        var lcti = GetTokenIssuer();

        // Valid names
        var ct = lcti.GetCheckpointToken("cb");
        Assert.Equal("cb", ct.Name);
        Assert.True(ct.Position > -1);

        var mt = lcti.GetMeasureToken("mc");
        Assert.Equal("mc", mt.Name);
        Assert.True(mt.Position > -1);

        var tt = lcti.GetTagToken("ta");
        Assert.Equal("ta", tt.Name);
        Assert.True(tt.Position > -1);
    }

    [Fact]
    public void TokenIssuer_InvalidNames()
    {
        var lcti = GetTokenIssuer();

        // Invalid names
        var ct = lcti.GetCheckpointToken("ta");
        Assert.Equal("ta", ct.Name);
        Assert.True(ct.Position == -1);

        var mt = lcti.GetMeasureToken("cb");
        Assert.Equal("cb", mt.Name);
        Assert.True(mt.Position == -1);

        var tt = lcti.GetTagToken("mc");
        Assert.Equal("mc", tt.Name);
        Assert.True(tt.Position == -1);
    }

    private LatencyContextTokenIssuer GetTokenIssuer()
    {
        var r = GetRegistry();
        var li = new LatencyInstrumentProvider(r);
        return new LatencyContextTokenIssuer(li);
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
