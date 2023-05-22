// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal sealed class HttpStandardResilienceOptionsCustomValidator : IValidateOptions<HttpStandardResilienceOptions>
{
    private const int CircuitBreakerTimeoutMultiplier = 2;

    public ValidateOptionsResult Validate(string? name, HttpStandardResilienceOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.AttemptTimeoutOptions.TimeoutInterval > options.TotalRequestTimeoutOptions.TimeoutInterval)
        {
            builder.AddError($"Total request timeout policy must have a greater timeout than the attempt timeout policy. " +
                $"Total Request Timeout: {options.TotalRequestTimeoutOptions.TimeoutInterval.TotalSeconds}s, " +
                $"Attempt Timeout: {options.AttemptTimeoutOptions.TimeoutInterval.TotalSeconds}s");
        }

        if (options.CircuitBreakerOptions.SamplingDuration < TimeSpan.FromMilliseconds(options.AttemptTimeoutOptions.TimeoutInterval.TotalMilliseconds * CircuitBreakerTimeoutMultiplier))
        {
            builder.AddError("The sampling duration of circuit breaker policy needs to be at least double of " +
                $"an attempt timeout policy’s timeout interval, in order to be effective. " +
                $"Sampling Duration: {options.CircuitBreakerOptions.SamplingDuration.TotalSeconds}s," +
                $"Attempt Timeout: {options.AttemptTimeoutOptions.TimeoutInterval.TotalSeconds}s");
        }

        if (options.RetryOptions.RetryCount != RetryPolicyOptions.InfiniteRetry)
        {
            TimeSpan retrySum = options.RetryOptions.GetRetryPolicyDelaySum();

            if (retrySum > options.TotalRequestTimeoutOptions.TimeoutInterval)
            {
                builder.AddError($"The cumulative delay of the retry policy cannot be larger than total request timeout policy interval. " +
                $"Cumulative Delay: {retrySum.TotalSeconds}s," +
                $"Total Request Timeout: {options.TotalRequestTimeoutOptions.TimeoutInterval.TotalSeconds}s");
            }
        }

        return builder.Build();

    }
}
