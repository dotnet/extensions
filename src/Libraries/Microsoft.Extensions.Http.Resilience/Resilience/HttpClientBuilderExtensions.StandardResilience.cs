// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    private const string StandardIdentifier = "standard";

    /// <summary>
    /// Adds a standard resilience handler that uses a multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience strategy combines multiple strategies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.AddStandardResilienceHandler().Configure(section);
    }

    /// <summary>
    /// Adds a standard resilience handler that uses a multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="configure">The callback that configures the options.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience strategy combines multiple strategies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.AddStandardResilienceHandler().Configure(configure);
    }

    /// <summary>
    /// Adds a standard resilience handler that uses a multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience strategy combines multiple strategies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var optionsName = StrategyNameHelper.GetName(builder.Name, StandardIdentifier);

        _ = builder.Services.AddValidatedOptions<HttpStandardResilienceOptions, HttpStandardResilienceOptionsCustomValidator>(optionsName);
        _ = builder.AddResilienceHandler(StandardIdentifier, (builder, context) =>
        {
            context.EnableReloads<HttpStandardResilienceOptions>(optionsName);

            var monitor = context.ServiceProvider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>();
            var options = monitor.Get(optionsName);

            _ = builder
                .AddRateLimiter(options.RateLimiterOptions)
                .AddTimeout(options.TotalRequestTimeoutOptions)
                .AddRetry(options.RetryOptions)
                .AddAdvancedCircuitBreaker(options.CircuitBreakerOptions)
                .AddTimeout(options.AttemptTimeoutOptions);
        });

        return new HttpStandardResilienceStrategyBuilder(optionsName, builder.Services);
    }

    private record HttpStandardResilienceStrategyBuilder(string StrategyName, IServiceCollection Services) : IHttpStandardResilienceStrategyBuilder;
}
