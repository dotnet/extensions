// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using Xunit;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[SupportedOSPlatform("windows")]
public class PerformanceCounterWrapperTests
{
    public PerformanceCounterWrapperTests()
    {
        Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Skipped on Linux/macOS");
    }

    [Fact]
    public void GetInstanceNameTest()
    {
        var wrapper = new PerformanceCounterWrapper("Processor", "% Processor Time", "_Total");
        Assert.Equal("_Total", wrapper.InstanceName);
    }
}
