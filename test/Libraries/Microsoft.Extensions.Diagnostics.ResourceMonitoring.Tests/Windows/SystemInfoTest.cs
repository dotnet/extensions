// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

/// <summary>
/// System Info Interop Tests.
/// </summary>
/// <remarks>These tests are added for coverage reasons, but the code doesn't have
/// the necessary environment predictability to really test it.</remarks>
public sealed class SystemInfoTest
{
    /// <summary>
    /// Get basic system info.
    /// </summary>
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
    public void GetSystemInfo()
    {
        var sysInfo = new SystemInfo().GetSystemInfo();
        Assert.True(sysInfo.NumberOfProcessors > 0);
    }
}
