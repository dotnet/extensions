// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Telemetry.Latency.Test;

public class LatencyConsoleOptionsTests
{
    [Fact]
    public void ConsoleExporterOptions_BasicTest()
    {
        var o = new LatencyConsoleOptions();
        Assert.True(o.OutputCheckpoints);
        Assert.True(o.OutputTags);
        Assert.True(o.OutputMeasures);

        o.OutputCheckpoints = false;
        o.OutputTags = false;
        o.OutputMeasures = false;

        Assert.False(o.OutputCheckpoints);
        Assert.False(o.OutputTags);
        Assert.False(o.OutputMeasures);
    }
}
