// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public sealed class ResourceMonitoringOptionsCustomValidatorTest
{
    [Fact]
    public void Test_ResourceMonitoringOptionsCustomValidator_With_Fake_IPv6_Address()
    {
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "[::]" }
        };
        var validator = new ResourceMonitoringOptionsCustomValidator();
        var result = validator.Validate("", options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Test_ResourceMonitoringOptionsCustomValidator_with_Fake_IPv4_Address()
    {
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "127.0.0.1" }
        };
        var validator = new ResourceMonitoringOptionsCustomValidator();
        var result = validator.Validate("", options);

        Assert.True(result.Succeeded);
    }
}
