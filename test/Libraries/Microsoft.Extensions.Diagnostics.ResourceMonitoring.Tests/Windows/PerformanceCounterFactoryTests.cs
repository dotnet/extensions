// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

[SupportedOSPlatform("windows")]
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public class PerformanceCounterFactoryTests
{
    [ConditionalFact]
    public void GetInstanceNameTest()
    {
        var performanceCounterFactory = new PerformanceCounterFactory();
        IPerformanceCounter performanceCounter = performanceCounterFactory.Create("Processor", "% Processor Time", "_Total");

        Assert.IsType<PerformanceCounterWrapper>(performanceCounter);
        Assert.Equal("_Total", performanceCounter.InstanceName);
    }
}
