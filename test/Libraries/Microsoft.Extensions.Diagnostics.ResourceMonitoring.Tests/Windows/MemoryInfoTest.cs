// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

/// <summary>
/// Memory Info Interop Tests.
/// </summary>
/// <remarks>These tests are added for coverage reasons, but the code doesn't have
/// the necessary environment predictability to really test it.</remarks>
public sealed class MemoryInfoTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void GetGlobalMemory()
    {
        var memoryStatus = new MemoryInfo().GetMemoryStatus();
        Assert.True(memoryStatus.TotalPhys > 0);
    }
}
