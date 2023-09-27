// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience.FaultInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpResilienceFaultInjectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers default implementations for <see cref="IFaultInjectionOptionsProvider"/>, <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddHttpClientFaultInjection(builder => builder.Configure());
    }

    /// <summary>
    /// Configures <see cref="FaultInjectionOptions"/> and registers default implementations for <see cref="IFaultInjectionOptionsProvider"/>,
    /// <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="section">The configuration section to bind to <see cref="FaultInjectionOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services,
        IConfiguration section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services.AddHttpClientFaultInjection(builder => builder.Configure(section));
    }

    /// <summary>
    /// Calls the given action to configure options with <see cref="HttpFaultInjectionOptionsBuilder"/> and registers default implementations for
    /// <see cref="IFaultInjectionOptionsProvider"/>, <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configure">Action to configure options with <see cref="HttpFaultInjectionOptionsBuilder"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// If the default instance of <see cref="IHttpClientFactory"/> is used, this method also adds a
    /// chaos policy handler to all registered <see cref="HttpClient"/> with its name as the identifier.
    /// Additional chaos policy handlers with different identifier names can be added using <see cref="HttpResilienceFaultInjectionHttpBuilderExtensions.AddFaultInjectionPolicyHandler"/>.
    /// </remarks>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, Action<HttpFaultInjectionOptionsBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        var builder = new HttpFaultInjectionOptionsBuilder(services);
        configure.Invoke(builder);

        _ = services.AddFaultInjection();
        services.TryAddSingleton<HttpClientFaultInjectionMetrics>();
        services.TryAddSingleton<IHttpClientChaosPolicyFactory, HttpClientChaosPolicyFactory>();
        services.TryAddSingleton<IHttpContentOptionsRegistry, HttpContentOptionsRegistry>();

        // Adds fault-injection for all Http Clients
        _ = services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                var chaosPolicyFactory = builder.Services.GetRequiredService<IChaosPolicyFactory>();
                var httpClientChaosPolicyFactory = builder.Services.GetRequiredService<IHttpClientChaosPolicyFactory>();
                var chaosPolicy = httpClientChaosPolicyFactory.CreateHttpResponsePolicy()
                    .WrapAsync(chaosPolicyFactory.CreateExceptionPolicy())
                    .WrapAsync(chaosPolicyFactory.CreateLatencyPolicy<HttpResponseMessage>());

                builder.AdditionalHandlers.Add(ActivatorUtilities.CreateInstance<FaultInjectionContextMessageHandler>(builder.Services, builder.Name!));
                builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(chaosPolicy));
            });
        });

        return services;
    }
}
