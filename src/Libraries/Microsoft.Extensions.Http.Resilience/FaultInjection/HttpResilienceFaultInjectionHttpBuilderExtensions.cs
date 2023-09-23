// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience.FaultInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpResilienceFaultInjectionHttpBuilderExtensions
{
    /// <summary>
    /// Adds a chaos policy handler identified by the chaos policy options group name to the given <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="chaosPolicyOptionsGroupName">The chaos policy options group name.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder builder, string chaosPolicyOptionsGroupName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(chaosPolicyOptionsGroupName);

        return builder
            .AddHttpMessageHandler(services => ActivatorUtilities.CreateInstance<FaultInjectionContextMessageHandler>(services, chaosPolicyOptionsGroupName))
            .AddChaosMessageHandler();
    }

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="IHttpClientBuilder"/>
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfig"/> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfig">Function to configure <see cref="FaultPolicyWeightAssignmentsOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    [Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(weightAssignmentsConfig);

        _ = builder.Services.Configure(builder.Name, weightAssignmentsConfig);

        return builder
            .AddHttpMessageHandler(services =>
            {
                var weightAssignmentsOptions = services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
                return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services, builder.Name, weightAssignmentsOptions);
            })
            .AddChaosMessageHandler();
    }

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="IHttpClientBuilder"/>
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfigSection"/> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfigSection">The configuration section to bind to <see cref="FaultPolicyWeightAssignmentsOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    [Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
        typeof(FaultPolicyWeightAssignmentsOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, IConfigurationSection weightAssignmentsConfigSection)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(weightAssignmentsConfigSection);

        _ = builder.Services.Configure<FaultPolicyWeightAssignmentsOptions>(builder.Name, weightAssignmentsConfigSection);

        return builder
            .AddHttpMessageHandler(services =>
            {
                var weightAssignmentsOptions = services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
                return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services, builder.Name, weightAssignmentsOptions);
            })
            .AddChaosMessageHandler();
    }

    private static IHttpClientBuilder AddChaosMessageHandler(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler(serviceProvider =>
        {
            var chaosPolicyFactory = serviceProvider.GetRequiredService<IChaosPolicyFactory>();
            var httpClientChaosPolicyFactory = serviceProvider.GetRequiredService<IHttpClientChaosPolicyFactory>();

            var policy = Policy.WrapAsync(
                chaosPolicyFactory.CreateLatencyPolicy<HttpResponseMessage>(),
                chaosPolicyFactory.CreateExceptionPolicy().AsAsyncPolicy<HttpResponseMessage>(),
                httpClientChaosPolicyFactory.CreateHttpResponsePolicy());

            return new PolicyHttpMessageHandler(policy);
        });
    }
}
