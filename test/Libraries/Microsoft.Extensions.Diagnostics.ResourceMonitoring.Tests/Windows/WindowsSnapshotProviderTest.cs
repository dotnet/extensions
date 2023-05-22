// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public sealed class WindowsSnapshotProviderTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void BasicConstructor()
    {
        var loggerMock = new Mock<ILogger<WindowsSnapshotProvider>>();
        var provider = new WindowsSnapshotProvider(loggerMock.Object);
        var memoryStatus = new MemoryInfo().GetMemoryStatus();

        Assert.Equal(Environment.ProcessorCount, provider.Resources.GuaranteedCpuUnits);
        Assert.Equal(Environment.ProcessorCount, provider.Resources.MaximumCpuUnits);
        Assert.Equal(memoryStatus.TotalPhys, provider.Resources.GuaranteedMemoryInBytes);
        Assert.Equal(memoryStatus.TotalPhys, provider.Resources.MaximumMemoryInBytes);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void GetSnapshot_DoesNotThrowExceptions()
    {
        var loggerMock = new Mock<ILogger<WindowsSnapshotProvider>>();
        var provider = new WindowsSnapshotProvider(loggerMock.Object);

        var exception = Record.Exception(() => provider.GetSnapshot());
        Assert.Null(exception);
    }
}
