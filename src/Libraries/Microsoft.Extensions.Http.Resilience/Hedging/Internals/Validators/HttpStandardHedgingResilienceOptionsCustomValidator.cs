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

        if (options.EndpointOptions.TimeoutOptions.Timeout > options.TotalRequestTimeoutOptions.Timeout)
        {
            builder.AddError($"Total request timeout strategy must have a greater timeout than the attempt timeout strategy. " +
                $"Total Request Timeout: {options.TotalRequestTimeoutOptions.Timeout.TotalSeconds}s, " +
                $"Attempt Timeout: {options.EndpointOptions.TimeoutOptions.Timeout.TotalSeconds}s");
        }

        var timeout = TimeSpan.FromMilliseconds(options.EndpointOptions.TimeoutOptions.Timeout.TotalMilliseconds * CircuitBreakerTimeoutMultiplier);
        if (options.EndpointOptions.CircuitBreakerOptions.SamplingDuration < timeout)
        {
            builder.AddError("The sampling duration of circuit breaker strategy needs to be at least double of " +
                $"an attempt timeout strategy’s timeout interval, in order to be effective. " +
                $"Sampling Duration: {options.EndpointOptions.CircuitBreakerOptions.SamplingDuration.TotalSeconds}s," +
                $"Attempt Timeout: {options.EndpointOptions.TimeoutOptions.Timeout.TotalSeconds}s");
        }

        // if generator is specified we cannot calculate the max hedging delay
        if (options.HedgingOptions.DelayGenerator == null)
        {
            var maxHedgingDelay = TimeSpan.FromMilliseconds(options.HedgingOptions.MaxHedgedAttempts * options.HedgingOptions.Delay.TotalMilliseconds);

            // Stryker disable once Equality
            if (maxHedgingDelay > options.TotalRequestTimeoutOptions.Timeout)
            {
                builder.AddError($"The cumulative delay of the hedging strategy is larger than total request timeout interval. " +
                    $"Total Request Timeout: {options.TotalRequestTimeoutOptions.Timeout.TotalSeconds}s, " +
                    $"Cumulative Hedging Delay: {maxHedgingDelay.TotalSeconds}s");
            }
        }

        return builder.Build();
    }
}
