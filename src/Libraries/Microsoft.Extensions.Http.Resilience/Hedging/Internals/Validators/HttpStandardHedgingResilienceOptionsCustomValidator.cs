// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal sealed class HttpStandardHedgingResilienceOptionsCustomValidator : IValidateOptions<HttpStandardHedgingResilienceOptions>
{
    private const int CircuitBreakerTimeoutMultiplier = 2;
    private readonly INamedServiceProvider<IRequestRoutingStrategyFactory> _namedServiceProvider;

    public HttpStandardHedgingResilienceOptionsCustomValidator(INamedServiceProvider<IRequestRoutingStrategyFactory> namedServiceProvider)
    {
        _namedServiceProvider = namedServiceProvider;
    }

    public ValidateOptionsResult Validate(string? name, HttpStandardHedgingResilienceOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (_namedServiceProvider.GetService(name!) is null)
        {
            builder.AddError($"The hedging routing is not configured for '{name}' HTTP client.");
        }

        if (options.EndpointOptions.TimeoutOptions.TimeoutInterval > options.TotalRequestTimeoutOptions.TimeoutInterval)
        {
            builder.AddError($"Total request timeout policy must have a greater timeout than the attempt timeout policy. " +
                $"Total Request Timeout: {options.TotalRequestTimeoutOptions.TimeoutInterval.TotalSeconds}s, " +
                $"Attempt Timeout: {options.EndpointOptions.TimeoutOptions.TimeoutInterval.TotalSeconds}s");
        }

        var timeout = TimeSpan.FromMilliseconds(options.EndpointOptions.TimeoutOptions.TimeoutInterval.TotalMilliseconds * CircuitBreakerTimeoutMultiplier);
        if (options.EndpointOptions.CircuitBreakerOptions.SamplingDuration < timeout)
        {
            builder.AddError("The sampling duration of circuit breaker policy needs to be at least double of " +
                $"an attempt timeout policy’s timeout interval, in order to be effective. " +
                $"Sampling Duration: {options.EndpointOptions.CircuitBreakerOptions.SamplingDuration.TotalSeconds}s," +
                $"Attempt Timeout: {options.EndpointOptions.TimeoutOptions.TimeoutInterval.TotalSeconds}s");
        }

        // if generator is specified we cannot calculate the max hedging delay
        if (options.HedgingOptions.HedgingDelayGenerator == null)
        {
            var maxHedgingDelay = TimeSpan.FromMilliseconds((options.HedgingOptions.MaxHedgedAttempts - 1) * options.HedgingOptions.HedgingDelay.TotalMilliseconds);

            // Stryker disable once Equality
            if (maxHedgingDelay > options.TotalRequestTimeoutOptions.TimeoutInterval)
            {
                builder.AddError($"The cumulative delay of the hedging policy is larger than total request timeout interval. " +
                    $"Total Request Timeout: {options.TotalRequestTimeoutOptions.TimeoutInterval.TotalSeconds}s, " +
                    $"Cumulative Hedging Delay: {maxHedgingDelay.TotalSeconds}s");
            }
        }

        return builder.Build();
    }
}
