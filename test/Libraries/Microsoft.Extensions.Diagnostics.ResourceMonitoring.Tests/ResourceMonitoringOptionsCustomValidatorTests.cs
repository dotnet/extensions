// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if NETCOREAPP3_1_OR_GREATER
using System.Linq;
#endif
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class ResourceMonitoringOptionsCustomValidatorTests
{
    [Fact]
    public void Test_WindowsCountersOptionsCustomValidator_With_Wrong_IP_Address()
    {
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "" }
        };
        var tcpTableInfo = new TcpTableInfo(Options.Options.Create(options));
        var validator = new ResourceMonitoringOptionsCustomValidator();
        var result = validator.Validate("", options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Test_WindowsCountersOptionsCustomValidator_with_Fake_IPv4_and_IPv6_Address()
    {
        var options = new ResourceMonitoringOptions
        {
            SourceIpAddresses = new HashSet<string> { "127.0.0.1", "[::]" }
        };
        var validator = new ResourceMonitoringOptionsCustomValidator();
        var result = validator.Validate("", options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(6, 5)]
    [InlineData(6, 6)]
    public void Validator_GivenValidOptions_Succeeds(int collectionWindow, int calculationPeriod)
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromSeconds(collectionWindow),
            PublishingWindow = TimeSpan.FromSeconds(calculationPeriod)
        };

        var validator = new ResourceMonitoringOptionsCustomValidator();
        var isValid = validator.Validate(nameof(options), options).Succeeded;
        Assert.True(isValid);
    }

    [Fact]
    public void Validator_CalculationPeriodBiggerThanCollectionWindow_Fails()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromSeconds(1),
            PublishingWindow = TimeSpan.FromSeconds(2)
        };

        var validator = new ResourceMonitoringOptionsCustomValidator();
        var validationResult = validator.Validate(nameof(options), options);

        Assert.True(validationResult.Failed);

#if NETCOREAPP3_1_OR_GREATER
        var failureMessage = validationResult.Failures.Single();
#else
        var failureMessage = validationResult.FailureMessage;
#endif
        Assert.Equal("Property PublishingWindow: Value must be <= to CollectionWindow (00:00:01), but is 00:00:02.", failureMessage);
    }
}
