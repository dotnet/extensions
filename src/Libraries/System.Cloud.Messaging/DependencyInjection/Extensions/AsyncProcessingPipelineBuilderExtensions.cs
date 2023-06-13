// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.DependencyInjection.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="IAsyncProcessingPipelineBuilder"/> to register:
///   1. singletons,
///   2. <see cref="IMessageSource"/>,
///   3. <see cref="IMessageDestination"/>,
///   4. <see cref="IMessageMiddleware"/>,
///   5. <see cref="MessageDelegate"/>,
///   6. <see cref="MessageConsumer"/>.
/// </summary>
public static class AsyncProcessingPipelineBuilderExtensions
{
    /// <summary>
    /// Adds any singletons required for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the singleton <typeparamref name="T"/> is already registered with the <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where T : class
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<T>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds any singletons required for the async processing pipeline with the provided <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for the singleton type.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                       Func<IServiceProvider, T> implementationFactory)
        where T : class
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds any singletons required for the async processing pipeline with the provided <paramref name="implementationFactory"/> against the provided <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="name">The name with which the singleton is registered.</param>
    /// <param name="implementationFactory">The implementation factory for the singleton type.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                       string name,
                                                                       Func<IServiceProvider, T> implementationFactory)
        where T : class
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNullOrEmpty(name);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton(name, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageDestination"/> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TDestination"/> is already registered with the <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TDestination : class, IMessageDestination
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageDestination>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TDestination>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageDestination"/> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="IMessageDestination"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                            Func<IServiceProvider, TDestination> implementationFactory)
        where TDestination : class, IMessageDestination
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageDestination>(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageDestination"/> for the async processing pipeline with the provided name and implementation factory.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="name">The name with which the <see cref="IMessageDestination"/> is registered with.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="IMessageDestination"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                            string name,
                                                                                            Func<IServiceProvider, TDestination> implementationFactory)
        where TDestination : class, IMessageDestination
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNullOrEmpty(name);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageDestination>(name, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageSource"/> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TSource"/> is already registered with the <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <typeparam name="TSource">Type of <see cref="IMessageSource"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TSource : class, IMessageSource
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageSource>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TSource>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageSource"/> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TSource">Type of <see cref="IMessageSource"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="IMessageSource"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                  Func<IServiceProvider, TSource> implementationFactory)
        where TSource : class, IMessageSource
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageSource>(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> to the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="IMessageMiddleware"/> in the pipeline is determined by the order of the calls to this method.
    /// Ensure the <typeparamref name="TMiddleware"/> is already registered with the <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="IMessageMiddleware"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TMiddleware : class, IMessageMiddleware
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TMiddleware>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> to the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="IMessageMiddleware"/> in the pipeline is determined by the order of the calls to this method.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="IMessageMiddleware"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="IMessageMiddleware"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                    Func<IServiceProvider, TMiddleware> implementationFactory)
        where TMiddleware : class, IMessageMiddleware
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the terminal <see cref="MessageDelegate"/> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="IMessageMiddleware"/> in the pipeline before calling this method via:
    ///  1. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder)"/> OR
    ///  2. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TMiddleware})"/>.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder)
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<MessageDelegate>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the terminal <see cref="MessageDelegate"/> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="IMessageMiddleware"/> in the pipeline before calling this method via:
    ///  1. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder)"/> OR
    ///  2. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TMiddleware})"/>.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="MessageDelegate"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                   Func<IServiceProvider, MessageDelegate> implementationFactory)
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="MessageConsumer"/> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TConsumer"/> is already registered with the <see cref="IServiceCollection"/>.
    /// </remarks>
    /// <typeparam name="TConsumer">Type of <see cref="MessageConsumer"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TConsumer : MessageConsumer
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<MessageConsumer>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TConsumer>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="MessageConsumer"/> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TConsumer">Type of <see cref="MessageConsumer"/> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="MessageConsumer"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                      Func<IServiceProvider, TConsumer> implementationFactory)
        where TConsumer : MessageConsumer
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<MessageConsumer>(pipelineBuilder.PipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the previously registered <see cref="MessageConsumer"/> for the async processing pipeline as a <see cref="BackgroundService"/>.
    /// </summary>
    /// <remarks>
    /// Ensure to configure the required <see cref="MessageConsumer"/> before calling this method via:
    ///   1. <see cref="ConfigureMessageConsumer{TConsumer}(IAsyncProcessingPipelineBuilder)"/> OR
    ///   2. <see cref="ConfigureMessageConsumer{TConsumer}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TConsumer})"/>.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static void RunConsumerAsBackgroundService(this IAsyncProcessingPipelineBuilder pipelineBuilder)
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = pipelineBuilder.Services.AddSingleton<IHostedService>(serviceProvider =>
        {
            INamedServiceProvider<MessageConsumer> namedMessageConsumerProvider = serviceProvider.GetRequiredService<INamedServiceProvider<MessageConsumer>>();
            MessageConsumer messageConsumer = namedMessageConsumerProvider.GetRequiredService(pipelineBuilder.PipelineName);
            return new ConsumerBackgroundService(messageConsumer);
        });
    }
}
