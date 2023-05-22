// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the delegate factory for <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <param name="factory"><see cref="IPipelineDelegateFactory"/> implementation.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IServiceCollection WithPipelineDelegateFactory(this IServiceCollection services, Func<IServiceProvider, IPipelineDelegateFactory> factory)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(factory);

        _ = services.AddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Create a message processing pipeline with the provided <paramref name="pipelineName"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="pipelineName">The name of the pipeline.</param>
    /// <returns>The <see cref="IAsyncProcessingPipelineBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddAsyncPipeline(this IServiceCollection services, string pipelineName)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNullOrEmpty(pipelineName);

        services.TryAddSingleton<IPipelineDelegateFactory>(sp => new PipelineDelegateFactory(sp));
        _ = services.AddNamedSingleton(pipelineName, sp =>
        {
            IPipelineDelegateFactory pipeline = sp.GetRequiredService<IPipelineDelegateFactory>();
            return pipeline.Create(pipelineName);
        });

        return new AsyncProcessingPipelineBuilder(pipelineName, services);
    }
}
