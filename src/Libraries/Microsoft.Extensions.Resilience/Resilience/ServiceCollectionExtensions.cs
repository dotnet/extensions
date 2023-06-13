// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Returns a generic <see cref="IResiliencePipelineBuilder{TResult}"/> that is used to configure the new resilience pipeline.
    /// </summary>
    /// <typeparam name="TPolicyResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="services">The DI container.</param>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IResiliencePipelineBuilder<TPolicyResult> AddResiliencePipeline<TPolicyResult>(
        this IServiceCollection services,
        string pipelineName)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNullOrEmpty(pipelineName);

        _ = PolicyFactoryServiceCollectionExtensions.AddPolicyFactory<TPolicyResult>(services);

        _ = services.AddValidatedOptions<ResiliencePipelineFactoryOptions<TPolicyResult>, ResiliencePipelineFactoryOptionsValidator<TPolicyResult>>(pipelineName);
        _ = services.AddOptions<ResiliencePipelineFactoryTokenSourceOptions<TPolicyResult>>(pipelineName);
        _ = services.AddSingleton<IOptionsChangeTokenSource<ResiliencePipelineFactoryOptions<TPolicyResult>>>(sp =>
            ActivatorUtilities.CreateInstance<PipelineConfigurationChangeTokenSource<TPolicyResult>>(sp, pipelineName));

        services.TryAddSingleton<IResiliencePipelineProvider, ResiliencePipelineProvider>();
        services.TryAddSingleton<IResiliencePipelineFactory, ResiliencePipelineFactory>();
        services.TryAddTransient<Internal.IPolicyPipelineBuilder<TPolicyResult>, PolicyPipelineBuilder<TPolicyResult>>();
        services.TryAddTransient<Internal.IPipelineMetering, PipelineMetering>();
        services.TryAddSingleton<IOnChangeListenersHandler, OnChangeListenersHandler>();

        _ = services.RegisterMetering();

        return new ResiliencePipelineBuilder<TPolicyResult>(services, pipelineName);
    }
}
