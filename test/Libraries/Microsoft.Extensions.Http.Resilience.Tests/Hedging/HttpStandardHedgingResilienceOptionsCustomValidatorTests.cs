// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public class HttpStandardHedgingResilienceOptionsCustomValidatorTests
{
    [Fact]
    public void Validate_InvalidOptions_EnsureValidationErrors()
    {
        HttpStandardHedgingResilienceOptions options = new();
        options.EndpointOptions.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromSeconds(1);
        options.TotalRequestTimeoutOptions.Timeout = TimeSpan.FromSeconds(1);

        var validationResult = CreateValidator("dummy").Validate("dummy", options);

        Assert.True(validationResult.Failed);

#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
        // and as such the API is available in .NET 8 onwards.
        Assert.Equal(3, validationResult.Failures.Count());
#endif
    }

    [Fact]
    public void Validate_ValidOptions_NoValidationErrors()
    {
        HttpStandardHedgingResilienceOptions options = new();

        var validationResult = CreateValidator("dummy").Validate("dummy", options);

        Assert.True(validationResult.Succeeded);
    }

    [Fact]
    public void Validate_ValidOptionsWithoutRouting_ValidationErrors()
    {
        HttpStandardHedgingResilienceOptions options = new();

        var validationResult = CreateValidator("dummy").Validate("other", options);

        Assert.True(validationResult.Failed);
        Assert.Equal("The hedging routing is not configured for 'other' HTTP client.", validationResult.FailureMessage);

    }

    public static IEnumerable<object[]> GetOptions_ValidOptions_EnsureNoErrors_Data
    {
        get
        {
            var options = new HttpStandardHedgingResilienceOptions();
            options.EndpointOptions.TimeoutOptions.Timeout = options.TotalRequestTimeoutOptions.Timeout;
            options.EndpointOptions.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromMilliseconds(options.EndpointOptions.TimeoutOptions.Timeout.TotalMilliseconds * 2);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.EndpointOptions.TimeoutOptions.Timeout = options.TotalRequestTimeoutOptions.Timeout;
            options.EndpointOptions.CircuitBreakerOptions.SamplingDuration =
                TimeSpan.FromMilliseconds(options.EndpointOptions.TimeoutOptions.Timeout.TotalMilliseconds * 2) + TimeSpan.FromMilliseconds(10);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.HedgingOptions.MaxHedgedAttempts = 1;
            options.HedgingOptions.HedgingDelay = options.TotalRequestTimeoutOptions.Timeout;
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.HedgingOptions.HedgingDelay = TimeSpan.FromDays(1);
            options.HedgingOptions.HedgingDelayGenerator = _ => new ValueTask<TimeSpan>(TimeSpan.FromDays(1));
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_ValidOptions_EnsureNoErrors_Data))]
    [Theory]
    public void Validate_ValidOptions_EnsureNoErrors(HttpStandardHedgingResilienceOptions options)
    {
        var validationResult = CreateValidator("dummy").Validate("dummy", options);

        Assert.False(validationResult.Failed);
    }

    public static IEnumerable<object[]> GetOptions_InvalidOptions_EnsureErrors_Data
    {
        get
        {
            var options = new HttpStandardHedgingResilienceOptions();
            options.TotalRequestTimeoutOptions.Timeout = TimeSpan.FromSeconds(2);
            options.EndpointOptions.TimeoutOptions.Timeout = TimeSpan.FromSeconds(3);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.TotalRequestTimeoutOptions.Timeout = TimeSpan.FromSeconds(2);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.HedgingOptions.HedgingDelay = TimeSpan.FromDays(1);
            yield return new object[] { options };

            options = new HttpStandardHedgingResilienceOptions();
            options.EndpointOptions.TimeoutOptions.Timeout = options.TotalRequestTimeoutOptions.Timeout;
            options.EndpointOptions.CircuitBreakerOptions.SamplingDuration = TimeSpan.FromMilliseconds(options.EndpointOptions.TimeoutOptions.Timeout.TotalMilliseconds / 2);
            yield return new object[] { options };
        }
    }

    [MemberData(nameof(GetOptions_InvalidOptions_EnsureErrors_Data))]
    [Theory]
    public void Validate_InvalidOptions_EnsureErrors(HttpStandardHedgingResilienceOptions options)
    {
        var validationResult = CreateValidator("dummy").Validate("dummy", options);

        Assert.True(validationResult.Failed);
    }

    private static HttpStandardHedgingResilienceOptionsCustomValidator CreateValidator(string name)
    {
        var mock = Mock.Of<INamedServiceProvider<IRequestRoutingStrategyFactory>>(v => v.GetService(name) == Mock.Of<IRequestRoutingStrategyFactory>());

        return new HttpStandardHedgingResilienceOptionsCustomValidator(mock);
    }
}
