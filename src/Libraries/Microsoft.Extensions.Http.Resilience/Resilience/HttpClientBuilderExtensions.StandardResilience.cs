// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    private const string StandardIdentifier = "standard";

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler" /> that uses a standard resilience pipeline with default options to send the requests and handle any transient errors.
    /// The pipeline combines multiple policies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual policies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.AddStandardResilienceHandler().Configure(section);
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler" /> that uses a standard resilience pipeline with default options to send the requests and handle any transient errors.
    /// The pipeline combines multiple policies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="configure">The action that configures the resilience options.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual policies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.AddStandardResilienceHandler().Configure(configure);
    }

    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler" /> that uses a standard resilience pipeline with default <see cref="HttpStandardResilienceOptions"/>
    /// to send the requests and handle any transient errors.
    /// The pipeline combines multiple policies that are configured based on HTTP-specific <see cref="HttpStandardResilienceOptions"/> options with recommended defaults.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// See <see cref="HttpStandardResilienceOptions"/> for more details about the individual policies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.ConfigureHttpFailureResultContext();

        return new HttpStandardResiliencePipelineBuilder(builder.AddResilienceHandler(StandardIdentifier).AddStandardPipeline());
    }

    private static HttpResiliencePipelineBuilder AddStandardPipeline(this IResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        var resilienceBuilder =
            builder.AddPolicy<HttpResponseMessage, HttpStandardResilienceOptions, HttpStandardResilienceOptionsValidator>(
            builder.PipelineName,
            options => { },
            (builder, options, _) =>
                builder
                    .AddBulkheadPolicy(StandardPolicyNames.Bulkhead, options.BulkheadOptions)
                    .AddTimeoutPolicy(StandardPolicyNames.TotalRequestTimeout, options.TotalRequestTimeoutOptions)
                    .AddRetryPolicy(StandardPolicyNames.Retry, options.RetryOptions)
                    .AddCircuitBreakerPolicy(StandardPolicyNames.CircuitBreaker, options.CircuitBreakerOptions)
                    .AddTimeoutPolicy(StandardPolicyNames.AttemptTimeout, options.AttemptTimeoutOptions));

        _ = builder.Services.AddValidatedOptions<HttpStandardResilienceOptions, HttpStandardResilienceOptionsCustomValidator>(builder.PipelineName);

        return new HttpResiliencePipelineBuilder(resilienceBuilder);
    }
}
