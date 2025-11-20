// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[SupportedOSPlatform("windows")]
[PlatformSpecific(TestPlatforms.Windows)]
public class PerformanceCounterWrapperTests
{
    [ConditionalFact]
    public void GetInstanceNameTest()
    {
        var wrapper = new PerformanceCounterWrapper("Processor", "% Processor Time", "_Total");
        Assert.Equal("_Total", wrapper.InstanceName);
    }
}
