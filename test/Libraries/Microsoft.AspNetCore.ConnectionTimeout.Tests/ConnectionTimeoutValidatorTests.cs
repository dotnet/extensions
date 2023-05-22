// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Connections.Test;

public class ConnectionTimeoutValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(60001)]
    [InlineData(99999)]
    public void ConnectionTimeoutValidator_GivenOutOfAllowedRangeTimeout_ReturnsValidationFailed(int seconds)
    {
        var validator = new ConnectionTimeoutValidator();
        var options = new ConnectionTimeoutOptions
        {
            Timeout = TimeSpan.FromSeconds(seconds)
        };

        var validationResult = validator.Validate(nameof(ConnectionTimeoutOptions), options);

        Assert.True(validationResult.Failed);
        Assert.Contains(nameof(ConnectionTimeoutOptions.Timeout), validationResult.FailureMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3600)]
    public void ConnectionTimeoutValidator_GivenAllowedRangeTimeout_ReturnsValidationSucceeded(int seconds)
    {
        var validator = new ConnectionTimeoutValidator();
        var options = new ConnectionTimeoutOptions
        {
            Timeout = TimeSpan.FromSeconds(seconds)
        };

        var validationResult = validator.Validate(nameof(ConnectionTimeoutOptions), options);

        Assert.True(validationResult.Succeeded, validationResult.FailureMessage);
    }
}
