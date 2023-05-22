// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    internal const string FallbackExperimentalMessage = "The current form of this API is experimental. " +
    "A direct replacement for it will be provided in the follow up versions of the SDK. " +
    "If you're a new adopter, consider using the Hedging handler. If you are already using the API, stay tuned for the next release's features.";

    /// <summary>
    /// Adds a fallback handler that wraps the execution of the request with a fallback mechanism,
    /// ensuring that the request is retried against a secondary endpoint.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">The configure callback.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    [Experimental(FallbackExperimentalMessage)]
    public static IHttpClientBuilder AddFallbackHandler(this IHttpClientBuilder builder, Action<FallbackClientHandlerOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.AddFallbackHandlerInternal(null, configure);
    }

    /// <summary>
    /// Adds a fallback handler that wraps the execution of the request with a fallback mechanism,
    /// ensuring that the request is retried against a secondary endpoint.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="section">The section that the <see cref="FallbackClientHandlerOptions"/> will bind against.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    [Experimental(FallbackExperimentalMessage)]
    public static IHttpClientBuilder AddFallbackHandler(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.AddFallbackHandlerInternal(section, null);
    }

    /// <summary>
    /// Adds a fallback handler that wraps the execution of the request with a fallback mechanism,
    /// ensuring that the request is retried against a secondary endpoint.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="section">The section that the <see cref="FallbackClientHandlerOptions"/> will bind against.</param>
    /// <param name="configure">The configure callback.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    [Experimental(FallbackExperimentalMessage)]
    public static IHttpClientBuilder AddFallbackHandler(this IHttpClientBuilder builder, IConfigurationSection section, Action<FallbackClientHandlerOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(configure);

        return builder.AddFallbackHandlerInternal(section, configure);
    }

    private static IHttpClientBuilder AddFallbackHandlerInternal(this IHttpClientBuilder builder, IConfigurationSection? section, Action<FallbackClientHandlerOptions>? configure)
    {
        FallbackHelper.AddFallbackPolicy(
            builder.AddResilienceHandler(FallbackHelper.HandlerPostfix),
            optionsName: builder.Name,
            options => options.Configure(section, configure));

        return builder;
    }
}
