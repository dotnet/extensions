// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Internals;

public class RetryPolicyOptionsCustomValidatorTests
{
    [InlineData(2_147_483_647, true)]
    [InlineData(2_147_483_648, false)]
    [Theory]
    public void Validate_Ok(long baseDelay, bool valid)
    {
        var options = new RetryPolicyOptions<string>
        {
            BaseDelay = TimeSpan.FromMilliseconds(baseDelay),
            BackoffType = BackoffType.Linear,
            RetryCount = 1
        };

        var validator = new RetryPolicyOptionsCustomValidator();
        var errors = validator.Validate("dummy", options);

        if (valid)
        {
            Assert.False(errors.Failed);
        }
        else
        {
            Assert.True(errors.Failed);
            Assert.Equal(
                $"Property RetryCount: unable to validate retry delay #0 = {baseDelay}. Must be a positive TimeSpan and less than {int.MaxValue} milliseconds long.",
                errors.FailureMessage);
        }
    }
}
