// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Moq;

namespace Microsoft.Extensions.Telemetry.Latency.Test;
internal static class MockLatencyContextRegistrationOptions
{
    public static IOptions<LatencyContextRegistrationOptions> GetLatencyContextRegistrationOptions(
        string[] checkpoints,
        string[] measures,
        string[] tags)
    {
        var options = new LatencyContextRegistrationOptions
        {
            CheckpointNames = checkpoints,
            MeasureNames = measures,
            TagNames = tags
        };

        var lcro = new Mock<IOptions<LatencyContextRegistrationOptions>>();
        lcro.Setup(a => a.Value).Returns(options);

        return lcro.Object;
    }
}
