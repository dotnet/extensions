// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Add extension methods to <see cref="IAsyncProcessingPipelineBuilder"/> to create <see cref="IMessageMiddleware"/> to record latency.
/// </summary>
public static class LatencyRecorderMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> implementation to register the <see cref="ILatencyContextProvider"/> with <see cref="MessageContext"/> for recording latency
    /// in the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder)
    {
        return AddLatencyContextMiddleware(pipelineBuilder, sp => sp.GetRequiredService<ILatencyContextProvider>(), sp => sp.GetServices<ILatencyDataExporter>());
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> implementation to register the <see cref="ILatencyContextProvider"/> with <see cref="MessageContext"/> for recording latency
    /// in the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ILatencyContextProvider"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">The <see cref="ILatencyContextProvider"/> implementation factory.</param>
    /// <param name="exporterFactory">The <see cref="IEnumerable{ILatencyDataExporter}"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                 Func<IServiceProvider, T> implementationFactory,
                                                                                 Func<IServiceProvider, IEnumerable<ILatencyDataExporter>> exporterFactory)
        where T : class, ILatencyContextProvider
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);
        _ = Throw.IfNull(exporterFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName,
                sp => new LatencyContextProviderMiddleware(implementationFactory(sp), exporterFactory(sp)));
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> for recording latency of the underlying <see cref="IMessageMiddleware"/> pipeline
    /// in the <see cref="ILatencyContext"/> associated with <see cref="MessageContext"/>.
    /// Refer <see cref="MessageLatencyContextFeatureExtensions.TryGetLatencyContext(MessageContext, out ILatencyContext?)"/>.
    /// </summary>
    /// <remarks>
    /// Ensure to register the <see cref="AddLatencyContextMiddleware(IAsyncProcessingPipelineBuilder)"/> OR
    /// <see cref="AddLatencyContextMiddleware{T}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, T}, Func{IServiceProvider, IEnumerable{ILatencyDataExporter}})"/>
    /// before calling this method.
    /// </remarks>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="successMeasureToken">Success <see cref="MeasureToken"/>.</param>
    /// <param name="failureMeasureToken">Failure <see cref="MeasureToken"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyRecorderMessageMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                      MeasureToken successMeasureToken,
                                                                                      MeasureToken failureMeasureToken)
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(successMeasureToken);
        _ = Throw.IfNull(failureMeasureToken);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName, sp => new LatencyRecorderMiddleware(successMeasureToken, failureMeasureToken));
        return pipelineBuilder;
    }
}
