// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Metering.Test;

public class MetricValueTests
{
    [Fact]
    public void Add_ThrowsWhenWrongValueTypeIsUsed()
    {
        var metricValue = new MetricValue<char>('1', Array.Empty<KeyValuePair<string, object?>>(), DateTimeOffset.Now);

        var ex = Assert.Throws<InvalidOperationException>(() => metricValue.Add('d'));
        Assert.Equal($"The type {typeof(char).FullName} is not supported as a metering measurement value type.", ex.Message);
    }
}
