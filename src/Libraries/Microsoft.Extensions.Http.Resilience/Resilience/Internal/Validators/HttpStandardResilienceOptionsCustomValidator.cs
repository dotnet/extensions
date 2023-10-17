// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal sealed class HttpStandardResilienceOptionsCustomValidator : IValidateOptions<HttpStandardResilienceOptions>
{
    private const int CircuitBreakerTimeoutMultiplier = 2;

    public ValidateOptionsResult Validate(string? name, HttpStandardResilienceOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.AttemptTimeout.Timeout > options.TotalRequestTimeout.Timeout)
        {
            builder.AddError($"Total request timeout resilience strategy must have a greater timeout than the attempt resilience strategy. " +
                $"Total Request Timeout: {options.TotalRequestTimeout.Timeout.TotalSeconds}s, " +
                $"Attempt Timeout: {options.AttemptTimeout.Timeout.TotalSeconds}s");
        }

        if (options.CircuitBreaker.SamplingDuration < TimeSpan.FromMilliseconds(options.AttemptTimeout.Timeout.TotalMilliseconds * CircuitBreakerTimeoutMultiplier))
        {
            builder.AddError("The sampling duration of circuit breaker strategy needs to be at least double of " +
                $"an attempt timeout strategy’s timeout interval, in order to be effective. " +
                $"Sampling Duration: {options.CircuitBreaker.SamplingDuration.TotalSeconds}s," +
                $"Attempt Timeout: {options.AttemptTimeout.Timeout.TotalSeconds}s");
        }

        return builder.Build();

    }
}
