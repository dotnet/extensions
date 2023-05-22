// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Logging.Test;

public class FakeLogCollectorOptionsTests
{
    [Fact]
    public void Defaults()
    {
        var options = new FakeLogCollectorOptions();
        Assert.Empty(options.FilteredCategories);
        Assert.Empty(options.FilteredLevels);
        Assert.True(options.CollectRecordsForDisabledLogLevels);
        Assert.Equal(System.TimeProvider.System, options.TimeProvider);
    }
}
