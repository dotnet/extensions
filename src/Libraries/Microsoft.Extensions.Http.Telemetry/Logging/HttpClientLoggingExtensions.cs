// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Extension methods to register HTTP client logging feature.
/// </summary>
public static class HttpClientLoggingExtensions
{
    internal static readonly string HandlerAddedTwiceExceptionMessage =
        $"{typeof(HttpLoggingHandler)} was already added either to all HttpClientBuilder's or to the current instance of {typeof(IHttpClientBuilder)}.";

    private static readonly ServiceDescriptor _removeDefaultLoggingFilterDescriptor = ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, BuiltInLoggingRemoverFilter>();

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        AddBuiltInLoggingRemoverFilter(services, name: null);

        services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return services.ConfigureAll<HttpClientFactoryOptions>(
            httpClientOptions =>
            {
                httpClientOptions
                .HttpMessageHandlerBuilderActions.Add(httpMessageHandlerBuilder =>
                {
                    var logger = httpMessageHandlerBuilder.Services.GetRequiredService<ILogger<HttpLoggingHandler>>();
                    var httpRequestReader = httpMessageHandlerBuilder.Services.GetRequiredService<IHttpRequestReader>();
                    var enrichers = httpMessageHandlerBuilder.Services.GetServices<IHttpClientLogEnricher>();
                    var loggingOptions = httpMessageHandlerBuilder.Services.GetRequiredService<IOptions<LoggingOptions>>();

                    if (httpMessageHandlerBuilder.AdditionalHandlers.Any(handler => handler is HttpLoggingHandler))
                    {
                        Throw.InvalidOperationException(HandlerAddedTwiceExceptionMessage);
                    }

                    httpMessageHandlerBuilder.AdditionalHandlers.Add(new HttpLoggingHandler(logger, httpRequestReader, enrichers, loggingOptions));
                });
            });
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(section);

        _ = services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>()
            .Bind(section);

        return services.AddDefaultHttpClientLogging();
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(configure);

        _ = services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>()
            .Configure(configure);

        return services.AddDefaultHttpClientLogging();
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        AddBuiltInLoggingRemoverFilter(builder.Services, builder.Name);

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        _ = builder.ConfigureHttpMessageHandlerBuilder(b =>
        {
            if (b.AdditionalHandlers.Any(handler => handler is HttpLoggingHandler))
            {
                Throw.InvalidOperationException(HandlerAddedTwiceExceptionMessage);
            }
        });

        return builder.AddHttpMessageHandler(ConfigureHandler(builder));
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name)
            .Bind(section);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        AddBuiltInLoggingRemoverFilter(builder.Services, builder.Name);

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return builder.AddHttpMessageHandler(ConfigureHandler(builder));
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name)
            .Configure(configure);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        AddBuiltInLoggingRemoverFilter(builder.Services, builder.Name);

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return builder.AddHttpMessageHandler(ConfigureHandler(builder));
    }

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T"/> to the <see cref="IServiceCollection"/> to enrich HTTP client logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the instance of <typeparamref name="T"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services)
        where T : class, IHttpClientLogEnricher
    {
        _ = Throw.IfNull(services);

        _ = services.AddActivatedSingleton<IHttpClientLogEnricher, T>();

        return services;
    }

    /// <summary>
    /// Configures DI registration so that a named instance of <see cref="LoggingOptions"/> gets injected into <see cref="HttpLoggingHandler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    private static Func<IServiceProvider, DelegatingHandler> ConfigureHandler(IHttpClientBuilder builder)
    {
        return serviceProvider =>
        {
            var loggingOptions = Microsoft.Extensions.Options.Options.Create(serviceProvider
                .GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(builder.Name));

            return ActivatorUtilities.CreateInstance<HttpLoggingHandler>(
                serviceProvider,
                ActivatorUtilities.CreateInstance<HttpRequestReader>(
                    serviceProvider,
                    ActivatorUtilities.CreateInstance<HttpHeadersReader>(
                        serviceProvider,
                        loggingOptions),
                    loggingOptions),
                loggingOptions);
        };
    }

    private static void AddBuiltInLoggingRemoverFilter(IServiceCollection services, string? name)
    {
        // We want to remove default logging. To do that we need to modify the builder after the filter that adds logging runs.
        // To do that we use another filter that runs after LoggingHttpMessageHandlerBuilderFilter. This is done by inserting
        // our filter to the service collection as the first item. That ensures it is in the right position when resolving
        // IHttpMessageHandlerBuilderFilter instances. It doesn't matter if AddHttpClient is called before or after.
        if (!services.Contains(_removeDefaultLoggingFilterDescriptor))
        {
            services.Insert(0, _removeDefaultLoggingFilterDescriptor);
        }

        _ = services.Configure<BuiltInLoggerRemoverFilterOptions>(o => o.ClientNames.Add(name));
    }

    private sealed class BuiltInLoggerRemoverFilterOptions
    {
        // Names of clients to remove built-in logging from.
        // A null value means built-in logging is removed globally from clients.
        public HashSet<string?> ClientNames { get; } = new HashSet<string?>();
    }

    private sealed class BuiltInLoggingRemoverFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly BuiltInLoggerRemoverFilterOptions _options;
        private readonly bool _global;

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "This constructor is used by dependency injection.")]
        public BuiltInLoggingRemoverFilter(IOptions<BuiltInLoggerRemoverFilterOptions> options)
        {
            _options = options.Value;
            _global = _options.ClientNames.Contains(null);
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return (builder) =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);

                if (_global || _options.ClientNames.Contains(builder.Name))
                {
                    // Remove the logger handlers added by the filter. Fortunately, they're both public, so it is a simple test on the type.
                    for (var i = builder.AdditionalHandlers.Count - 1; i >= 0; i--)
                    {
                        var handlerType = builder.AdditionalHandlers[i].GetType();
                        if (handlerType == typeof(LoggingScopeHttpMessageHandler) || handlerType == typeof(LoggingHttpMessageHandler))
                        {
                            builder.AdditionalHandlers.RemoveAt(i);
                        }
                    }
                }
            };
        }
    }
}
