// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal sealed class HttpStandardHedgingResilienceOptionsCustomValidator : IValidateOptions<HttpStandardHedgingResilienceOptions>
{
    private const int CircuitBreakerTimeoutMultiplier = 2;

    public ValidateOptionsResult Validate(string? name, HttpStandardHedgingResilienceOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.Endpoint.Timeout.Timeout > options.TotalRequestTimeout.Timeout)
        {
            builder.AddError($"Total request timeout strategy must have a greater timeout than the attempt timeout strategy. " +
                $"Total Request Timeout: {options.TotalRequestTimeout.Timeout.TotalSeconds}s, " +
                $"Attempt Timeout: {options.Endpoint.Timeout.Timeout.TotalSeconds}s");
        }

        var timeout = TimeSpan.FromMilliseconds(options.Endpoint.Timeout.Timeout.TotalMilliseconds * CircuitBreakerTimeoutMultiplier);
        if (options.Endpoint.CircuitBreaker.SamplingDuration < timeout)
        {
            builder.AddError("The sampling duration of circuit breaker strategy needs to be at least double of " +
                $"an attempt timeout strategy’s timeout interval, in order to be effective. " +
                $"Sampling Duration: {options.Endpoint.CircuitBreaker.SamplingDuration.TotalSeconds}s," +
                $"Attempt Timeout: {options.Endpoint.Timeout.Timeout.TotalSeconds}s");
        }

        // if generator is specified we cannot calculate the max hedging delay
        if (options.Hedging.DelayGenerator == null)
        {
            var maxHedgingDelay = TimeSpan.FromMilliseconds(options.Hedging.MaxHedgedAttempts * options.Hedging.Delay.TotalMilliseconds);

            // Stryker disable once Equality
            if (maxHedgingDelay > options.TotalRequestTimeout.Timeout)
            {
                builder.AddError($"The cumulative delay of the hedging strategy is larger than total request timeout interval. " +
                    $"Total Request Timeout: {options.TotalRequestTimeout.Timeout.TotalSeconds}s, " +
                    $"Cumulative Hedging Delay: {maxHedgingDelay.TotalSeconds}s");
            }
        }

        return builder.Build();
    }
}
