// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Latency;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to add support for the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> implementation to record latency.
/// </summary>
public static class LatencyRecorderMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to register the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContextProvider" /> in <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> pipeline to
    /// create and set <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> with the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <remarks>
    /// If the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> is already available in the workflow,
    /// use the <see cref="M:System.Cloud.Messaging.LatencyRecorderMiddlewareExtensions.AddLatencyContextMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" /> variant.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder);

    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to register the provided <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContextProvider" /> in the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> pipeline to
    /// create and set <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> with the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <remarks>
    /// If the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> is already available in the workflow,
    /// use the <see cref="M:System.Cloud.Messaging.LatencyRecorderMiddlewareExtensions.AddLatencyContextMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" /> variant.
    /// </remarks>
    /// <typeparam name="T">The type of <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContextProvider" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContextProvider" /> implementation factory.</param>
    /// <param name="exporterFactory">The factory for exporting capture latency context.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory, Func<IServiceProvider, IEnumerable<ILatencyDataExporter>> exporterFactory) where T : class, ILatencyContextProvider;

    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to reuse the existing <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> registered with the ASP.NET pipeline and set it in the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> implementation factory.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory) where T : class, ILatencyContext;

    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> for recording latency of the underlying <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> pipeline
    /// by obtaining the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> associated with <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <remarks>
    /// Ensure to register the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> in the <see cref="T:System.Cloud.Messaging.MessageContext" /> before calling this method via either of the following methods:
    ///   1. <see cref="M:System.Cloud.Messaging.LatencyRecorderMiddlewareExtensions.AddLatencyContextMiddleware(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder)" /> OR
    ///   2. <see cref="M:System.Cloud.Messaging.LatencyRecorderMiddlewareExtensions.AddLatencyContextMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0},System.Func{System.IServiceProvider,System.Collections.Generic.IEnumerable{Microsoft.Extensions.Telemetry.Latency.ILatencyDataExporter}})" /> OR
    ///   3. <see cref="M:System.Cloud.Messaging.LatencyRecorderMiddlewareExtensions.AddLatencyContextMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" />.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="successMeasureToken">The success token.</param>
    /// <param name="failureMeasureToken">The failure token.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddLatencyRecorderMessageMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder, MeasureToken successMeasureToken, MeasureToken failureMeasureToken);
}
