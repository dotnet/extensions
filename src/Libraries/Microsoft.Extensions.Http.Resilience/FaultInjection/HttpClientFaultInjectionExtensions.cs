// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpClientFaultInjectionExtensions
{
    /// <summary>
    /// Registers default implementations for <see cref="IFaultInjectionOptionsProvider"/>, <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        Action<HttpFaultInjectionOptionsBuilder> action = builder => builder.Configure();
        return services.AddHttpClientFaultInjection(action);
    }

    /// <summary>
    /// Configures <see cref="FaultInjectionOptions"/> and registers default implementations for <see cref="IFaultInjectionOptionsProvider"/>,
    /// <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="section">The configuration section to bind to <see cref="FaultInjectionOptions"/>.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services,
        IConfiguration section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        Action<HttpFaultInjectionOptionsBuilder> action = builder => builder.Configure(section);
        return services.AddHttpClientFaultInjection(action);
    }

    /// <summary>
    /// Calls the given action to configure options with <see cref="HttpFaultInjectionOptionsBuilder"/> and registers default implementations for
    /// <see cref="IFaultInjectionOptionsProvider"/>, <see cref="IChaosPolicyFactory"/> and <see cref="IHttpClientChaosPolicyFactory"/>;
    /// adds fault-injection policies to all <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configure">Action to configure options with <see cref="HttpFaultInjectionOptionsBuilder"/>.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// If the default instance of <see cref="IHttpClientFactory"/> is used, this method also adds a
    /// chaos policy handler to all registered <see cref="System.Net.Http.HttpClient"/> with its name as the identifier.
    /// Additional chaos policy handlers with different identifier names can be added using <see cref="AddFaultInjectionPolicyHandler"/>.
    /// </remarks>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services,
        Action<HttpFaultInjectionOptionsBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        var builder = new HttpFaultInjectionOptionsBuilder(services);
        configure.Invoke(builder);

        _ = services.AddFaultInjection();
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

                builder.AdditionalHandlers.Add(
                    ActivatorUtilities.CreateInstance<FaultInjectionContextMessageHandler>(builder.Services,
                        builder.Name!));
                builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(chaosPolicy));
            });
        });

        return services;
    }

    /// <summary>
    /// Adds a chaos policy handler identified by the chaos policy options group name to the given <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="chaosPolicyOptionsGroupName">The chaos policy options group name.</param>
    /// <returns>
    /// The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder httpClientBuilder,
        string chaosPolicyOptionsGroupName)
    {
        _ = Throw.IfNull(httpClientBuilder);
        _ = Throw.IfNullOrEmpty(chaosPolicyOptionsGroupName);

        _ = httpClientBuilder.AddHttpMessageHandler(services =>
        {
            return ActivatorUtilities.CreateInstance<FaultInjectionContextMessageHandler>(services,
                chaosPolicyOptionsGroupName);
        });

        return AddChaosMessageHandler(httpClientBuilder);
    }

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="IHttpClientBuilder"/>
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfig"/> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfig">Function to configure <see cref="FaultPolicyWeightAssignmentsOptions"/>.</param>
    /// <returns>
    /// The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.
    /// </returns>
    [Experimental]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder,
        Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig)
    {
        _ = Throw.IfNull(httpClientBuilder);
        _ = Throw.IfNull(weightAssignmentsConfig);

        _ = httpClientBuilder.Services.Configure(httpClientBuilder.Name, weightAssignmentsConfig);
        _ = httpClientBuilder.AddHttpMessageHandler(services =>
        {
            var weightAssignmentsOptions =
                services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
            return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services,
                httpClientBuilder.Name, weightAssignmentsOptions);
        });

        return httpClientBuilder.AddChaosMessageHandler();
    }

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="IHttpClientBuilder"/>
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfigSection"/> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfigSection">The configuration section to bind to <see cref="FaultPolicyWeightAssignmentsOptions"/>.</param>
    /// <returns>
    /// The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.
    /// </returns>
    [Experimental]
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
        typeof(FaultPolicyWeightAssignmentsOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder,
        IConfigurationSection weightAssignmentsConfigSection)
    {
        _ = Throw.IfNull(httpClientBuilder);
        _ = Throw.IfNull(weightAssignmentsConfigSection);

        _ = httpClientBuilder.Services.Configure<FaultPolicyWeightAssignmentsOptions>(httpClientBuilder.Name,
            weightAssignmentsConfigSection);
        _ = httpClientBuilder.AddHttpMessageHandler(services =>
        {
            var weightAssignmentsOptions =
                services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
            return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services,
                httpClientBuilder.Name, weightAssignmentsOptions);
        });

        return httpClientBuilder.AddChaosMessageHandler();
    }

    private static IHttpClientBuilder AddChaosMessageHandler(this IHttpClientBuilder httpClientBuilder)
    {
        _ = httpClientBuilder
            .AddResilienceHandler("chaos")
            .AddPolicy((pipelineBuilder, services) =>
            {
                var chaosPolicyFactory = services.GetRequiredService<IChaosPolicyFactory>();
                var httpClientChaosPolicyFactory = services.GetRequiredService<IHttpClientChaosPolicyFactory>();
                _ = pipelineBuilder
                    .AddPolicy(httpClientChaosPolicyFactory.CreateHttpResponsePolicy())
                    .AddPolicy(chaosPolicyFactory.CreateExceptionPolicy())
                    .AddPolicy(chaosPolicyFactory.CreateLatencyPolicy<HttpResponseMessage>());
            });

        return httpClientBuilder;
    }
}
