// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods to <see cref="IServiceProvider"/> to get <see cref="IMessageSource"/>, <see cref="IReadOnlyList{IMessageMiddleware}"/> and <see cref="MessageDelegate"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the message source for the provided async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure that the <see cref="IMessageSource"/> is registered with the provided <paramref name="pipelineName"/> in <see cref="IServiceCollection"/> via
    /// <see cref="AsyncProcessingPipelineBuilderExtensions.ConfigureMessageConsumer{TConsumer}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TConsumer})"/> or its variant.
    /// </remarks>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The message source.</returns>
    public static IMessageSource GetMessageSource(this IServiceProvider serviceProvider, string pipelineName)
    {
        _ = Throw.IfNull(serviceProvider);
        _ = Throw.IfNullOrEmpty(pipelineName);

        return serviceProvider.GetRequiredKeyedService<IMessageSource>(pipelineName);
    }

    /// <summary>
    /// Gets the list of message middleware for the provided async processing pipeline.
    /// </summary>
    /// Ensure that the <see cref="IMessageMiddleware"/> implementations are registered with the provided <paramref name="pipelineName"/> in <see cref="IServiceCollection"/> via
    /// <see cref="AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TMiddleware})"/> or its variant.
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The list of message middleware.</returns>
    public static IReadOnlyList<IMessageMiddleware> GetMessageMiddlewares(this IServiceProvider serviceProvider, string pipelineName)
    {
        _ = Throw.IfNull(serviceProvider);
        _ = Throw.IfNullOrEmpty(pipelineName);

        return serviceProvider.GetKeyedServices<IMessageMiddleware>(pipelineName).ToArray();
    }

    /// <summary>
    /// Gets the terminal message delegate for the provided async processing pipeline.
    /// </summary>
    /// Ensure that the <see cref="MessageDelegate"/> is registered with the provided <paramref name="pipelineName"/> in <see cref="IServiceCollection"/> via
    /// <see cref="AsyncProcessingPipelineBuilderExtensions.ConfigureTerminalMessageDelegate(IAsyncProcessingPipelineBuilder)"/> or its variant.
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The terminal message delegate.</returns>
    public static MessageDelegate GetMessageDelegate(this IServiceProvider serviceProvider, string pipelineName)
    {
        _ = Throw.IfNull(serviceProvider);
        _ = Throw.IfNullOrEmpty(pipelineName);

        return serviceProvider.GetRequiredKeyedService<MessageDelegate>(pipelineName);
    }
}
