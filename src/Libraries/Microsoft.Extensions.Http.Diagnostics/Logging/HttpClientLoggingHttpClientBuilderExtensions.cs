// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register extended HTTP client logging features.
/// </summary>
public static class HttpClientLoggingHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return AddExtendedHttpClientLoggingInternal(builder);
    }

    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return AddExtendedHttpClientLoggingInternal(builder, options => options.Bind(section));
    }

    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return AddExtendedHttpClientLoggingInternal(builder, options => options.Configure(configure));
    }

    private static IHttpClientBuilder AddExtendedHttpClientLoggingInternal(IHttpClientBuilder builder, Action<OptionsBuilder<LoggingOptions>>? configureOptionsBuilder = null)
    {
        var optionsBuilder = builder.Services
            .AddOptionsWithValidateOnStart<LoggingOptions, LoggingOptionsValidator>(builder.Name);

        configureOptionsBuilder?.Invoke(optionsBuilder);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        builder.Services.TryAddKeyedSingleton<HttpClientLogger>(builder.Name);
        builder.Services.TryAddKeyedSingleton<IHttpRequestReader, HttpRequestReader>(builder.Name);
        builder.Services.TryAddKeyedSingleton<IHttpHeadersReader, HttpHeadersReader>(builder.Name);

        return builder
            .RemoveAllLoggers()
            .AddLogger(
                serviceProvider => serviceProvider.GetRequiredKeyedService<HttpClientLogger>(builder.Name),
                wrapHandlersPipeline: true);
    }
}
