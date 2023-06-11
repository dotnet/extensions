// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Middlewares.Internal;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="IAsyncProcessingPipelineBuilder"/> to add support for the <see cref="IMessageMiddleware"/> implementation to record latency.
/// </summary>
public static class LatencyRecorderMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> to register the <see cref="ILatencyContextProvider"/> in <see cref="IMessageMiddleware"/> pipeline to
    /// create and set <see cref="ILatencyContext"/> with the <see cref="MessageContext"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="ILatencyContext"/> is already available in the workflow,
    /// use the <see cref="AddLatencyContextMiddleware{T}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, T})"/> variant.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder)
    {
        return AddLatencyContextMiddleware(pipelineBuilder, sp => sp.GetRequiredService<ILatencyContextProvider>(), sp => sp.GetServices<ILatencyDataExporter>());
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> to register the provided <see cref="ILatencyContextProvider"/> in the <see cref="IMessageMiddleware"/> pipeline to
    /// create and set <see cref="ILatencyContext"/> with the <see cref="MessageContext"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="ILatencyContext"/> is already available in the workflow,
    /// use the <see cref="AddLatencyContextMiddleware{T}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, T})"/> variant.
    /// </remarks>
    /// <typeparam name="T">The type of <see cref="ILatencyContextProvider"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The <see cref="ILatencyContextProvider"/> implementation factory.</param>
    /// <param name="exporterFactory">The factory for exporting capture latency context.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
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
    /// Adds the <see cref="IMessageMiddleware"/> to reuse the existing <see cref="ILatencyContext"/> registered with the ASP.NET pipeline and set it in the <see cref="MessageContext"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ILatencyContext"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The <see cref="ILatencyContext"/> implementation factory.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                 Func<IServiceProvider, T> implementationFactory)
        where T : class, ILatencyContext
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName,
                sp => new LatencyContextMiddleware(implementationFactory(sp)));
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> for recording latency of the underlying <see cref="IMessageMiddleware"/> pipeline
    /// by obtaining the <see cref="ILatencyContext"/> associated with <see cref="MessageContext"/>.
    /// </summary>
    /// <remarks>
    /// Ensure to register the <see cref="ILatencyContext"/> in the <see cref="MessageContext"/> before calling this method via either of the following methods:
    ///   1. <see cref="AddLatencyContextMiddleware(IAsyncProcessingPipelineBuilder)"/> OR
    ///   2. <see cref="AddLatencyContextMiddleware{T}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, T}, Func{IServiceProvider, IEnumerable{ILatencyDataExporter}})"/> OR
    ///   3. <see cref="AddLatencyContextMiddleware{T}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, T})"/>.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="successMeasureToken">The success token.</param>
    /// <param name="failureMeasureToken">The failure token.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
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
