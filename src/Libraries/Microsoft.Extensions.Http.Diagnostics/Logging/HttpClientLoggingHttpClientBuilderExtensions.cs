// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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
/// Extension methods to register HTTP client logging feature.
/// </summary>
public static class HttpClientLoggingHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return AddNamedClientLoggingInternal(builder);
    }

    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return AddNamedClientLoggingInternal(builder, options => options.Bind(section));
    }

    /// <summary>
    /// Adds an <see cref="IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="HttpClientBuilderExtensions.AddDefaultLogger(IHttpClientBuilder)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return AddNamedClientLoggingInternal(builder, options => options.Configure(configure));
    }

    private static IHttpClientBuilder AddNamedClientLoggingInternal(IHttpClientBuilder builder, Action<OptionsBuilder<LoggingOptions>>? configureOptionsBuilder = null)
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
