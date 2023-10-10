// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public class HttpStandardHedgingResilienceOptionsCustomValidatorTests
{
    [Fact]
    public void Validate_InvalidOptions_EnsureValidationErrors()
    {
        HttpStandardHedgingResilienceOptions options = new();
        options.Endpoint.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(1);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(1);

        var validationResult = CreateValidator().Validate("dummy", options);

        Assert.True(validationResult.Failed);

#if NET6_0_OR_GREATER
        validationResult.Failures.Should().HaveCount(3);
#endif
    }

    [Fact]
    public void Validate_ValidOptions_NoValidationErrors()
    {
        HttpStandardHedgingResilienceOptions options = new();

        var validationResult = CreateValidator().Validate("dummy", options);

        validationResult.Succeeded.Should().BeTrue();
    }

    public static IEnumerable<object[]> GetOptions_ValidOptions_EnsureNoErrors_Data
    {
        get
        {
            var options = new HttpStandardHedgingResilienceOptions();
            options.Endpoint.Timeout.Timeout = options.TotalRequestTimeout.Timeout;
            options.Endpoint.CircuitBreaker.SamplingDuration = TimeSpan.FromMilliseconds(options.Endpoint.Timeout.Timeout.TotalMilliseconds * 2);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.Endpoint.Timeout.Timeout = options.TotalRequestTimeout.Timeout;
            options.Endpoint.CircuitBreaker.SamplingDuration =
                TimeSpan.FromMilliseconds(options.Endpoint.Timeout.Timeout.TotalMilliseconds * 2) + TimeSpan.FromMilliseconds(10);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.Hedging.MaxHedgedAttempts = 1;
            options.Hedging.Delay = options.TotalRequestTimeout.Timeout;
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.Hedging.Delay = TimeSpan.FromDays(1);
            options.Hedging.DelayGenerator = _ => new ValueTask<TimeSpan>(TimeSpan.FromDays(1));
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_ValidOptions_EnsureNoErrors_Data))]
    [Theory]
    public void Validate_ValidOptions_EnsureNoErrors(HttpStandardHedgingResilienceOptions options)
    {
        var validationResult = CreateValidator().Validate("dummy", options);

        Assert.False(validationResult.Failed);
    }

    public static IEnumerable<object[]> GetOptions_InvalidOptions_EnsureErrors_Data
    {
        get
        {
            var options = new HttpStandardHedgingResilienceOptions();
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);
            options.Endpoint.Timeout.Timeout = TimeSpan.FromSeconds(3);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.Endpoint.Timeout.Timeout = options.TotalRequestTimeout.Timeout;
            options.Endpoint.CircuitBreaker.SamplingDuration = TimeSpan.FromMilliseconds(options.Endpoint.Timeout.Timeout.TotalMilliseconds / 2);
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_InvalidOptions_EnsureErrors_Data))]
    [Theory]
    public void Validate_InvalidOptions_EnsureErrors(HttpStandardHedgingResilienceOptions options)
    {
        var validationResult = CreateValidator().Validate("dummy", options);

        Assert.True(validationResult.Failed);
    }

    private static HttpStandardHedgingResilienceOptionsCustomValidator CreateValidator()
    {
        return new HttpStandardHedgingResilienceOptionsCustomValidator();
    }
}
