// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using System.Linq;
#endif
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals.Validators;
public class HttpStandardResilienceOptionsCustomValidatorTests
{
    [Fact]
    public void Validate_InvalidOptions_EnsureValidationErrors()
    {
        HttpStandardResilienceOptions options = new();
        options.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromSeconds(1);
        options.TotalRequestTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(1);

        var validationResult = new HttpStandardResilienceOptionsCustomValidator().Validate(string.Empty, options);

        Assert.True(validationResult.Failed);

#if NET6_0_OR_GREATER
        Assert.Equal(3, validationResult.Failures.Count());
#endif
    }

    [Fact]
    public void Validate_ValidOptions_NoValidationErrors()
    {
        HttpStandardResilienceOptions options = new();

        var validationResult = new HttpStandardResilienceOptionsCustomValidator().Validate(string.Empty, options);

        Assert.True(validationResult.Succeeded);
    }

    public static IEnumerable<object[]> GetOptions_ValidOptions_EnsureNoErrors_Data
    {
        get
        {
            var options = new HttpStandardResilienceOptions();
            options.AttemptTimeoutOptions.TimeoutInterval = options.TotalRequestTimeoutOptions.TimeoutInterval;
            options.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromMilliseconds(options.AttemptTimeoutOptions.TimeoutInterval.TotalMilliseconds * 2);
            yield return new object[] { options };

            options = new HttpStandardResilienceOptions();
            options.AttemptTimeoutOptions.TimeoutInterval = options.TotalRequestTimeoutOptions.TimeoutInterval;
            options.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromMilliseconds(options.AttemptTimeoutOptions.TimeoutInterval.TotalMilliseconds * 2) + TimeSpan.FromMilliseconds(10);
            yield return new object[] { options };

            options = new HttpStandardResilienceOptions();
            options.RetryOptions.RetryCount = 1;
            options.RetryOptions.BackoffType = BackoffType.Linear;
            options.RetryOptions.BaseDelay = options.TotalRequestTimeoutOptions.TimeoutInterval;
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_ValidOptions_EnsureNoErrors_Data))]
    [Theory]
    public void Validate_ValidOptions_EnsureNoErrors(HttpStandardResilienceOptions options)
    {
        var validationResult = new HttpStandardResilienceOptionsCustomValidator().Validate(string.Empty, options);

        Assert.False(validationResult.Failed);
    }

    public static IEnumerable<object[]> GetOptions_InvalidOptions_EnsureErrors_Data
    {
        get
        {
            var options = new HttpStandardResilienceOptions();
            options.TotalRequestTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(2);
            options.AttemptTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(3);
            yield return new object[] { options };

            options = new HttpStandardResilienceOptions();
            options.TotalRequestTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(2);
            yield return new object[] { options };

            options = new HttpStandardResilienceOptions();
            options.RetryOptions.BaseDelay = TimeSpan.FromDays(1);
            yield return new object[] { options };

            options = new HttpStandardResilienceOptions();
            options.AttemptTimeoutOptions.TimeoutInterval = options.TotalRequestTimeoutOptions.TimeoutInterval;
            options.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromMilliseconds(options.AttemptTimeoutOptions.TimeoutInterval.TotalMilliseconds / 2);
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_InvalidOptions_EnsureErrors_Data))]
    [Theory]
    public void Validate_InvalidOptions_EnsureErrors(HttpStandardResilienceOptions options)
    {
        var validationResult = new HttpStandardResilienceOptionsCustomValidator().Validate(string.Empty, options);

        Assert.True(validationResult.Failed);
    }
}
