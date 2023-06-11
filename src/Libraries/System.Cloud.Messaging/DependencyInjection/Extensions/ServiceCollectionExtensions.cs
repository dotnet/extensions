// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.DependencyInjection.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to create async processing pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Create an async processing pipeline with the provided <paramref name="pipelineName"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The builder for async processing pipeline.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddAsyncPipeline(this IServiceCollection services, string pipelineName)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNullOrEmpty(pipelineName);

        return new AsyncProcessingPipelineBuilder(pipelineName, services);
    }
}
