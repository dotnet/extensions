// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods to <see cref="T:System.IServiceProvider" /> to get <see cref="T:System.Cloud.Messaging.IMessageSource" />, <see cref="T:System.Collections.Generic.IReadOnlyList`1" /> and <see cref="T:System.Cloud.Messaging.MessageDelegate" />.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the message source for the provided async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure that the <see cref="T:System.Cloud.Messaging.IMessageSource" /> is registered with the provided <paramref name="pipelineName" /> in <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> via
    /// <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.ConfigureMessageConsumer``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" /> or its variant.
    /// </remarks>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The message source.</returns>
    public static IMessageSource GetMessageSource(this IServiceProvider serviceProvider, string pipelineName);

    /// <summary>
    /// Gets the list of message middleware for the provided async processing pipeline.
    /// </summary>
    /// Ensure that the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> implementations are registered with the provided <paramref name="pipelineName" /> in <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> via
    /// <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" /> or its variant.
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The list of message middleware.</returns>
    public static IReadOnlyList<IMessageMiddleware> GetMessageMiddlewares(this IServiceProvider serviceProvider, string pipelineName);

    /// <summary>
    /// Gets the terminal message delegate for the provided async processing pipeline.
    /// </summary>
    /// Ensure that the <see cref="T:System.Cloud.Messaging.MessageDelegate" /> is registered with the provided <paramref name="pipelineName" /> in <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> via
    /// <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.ConfigureTerminalMessageDelegate(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder)" /> or its variant.
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The terminal message delegate.</returns>
    public static MessageDelegate GetMessageDelegate(this IServiceProvider serviceProvider, string pipelineName);
}
