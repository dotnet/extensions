// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="IAsyncProcessingPipelineBuilder"/>.
/// </summary>
public static class AsyncProcessingPipelineBuilderExtensions
{
    /// <summary>
    /// Register any singletons required against the <see cref="IAsyncProcessingPipelineBuilder.PipelineName"/>.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where T : class
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<T>(pipelineBuilder.PipelineName);
        return pipelineBuilder;
    }

    /// <summary>
    /// Add any singletons required with the provided <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="T"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
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
    /// Add any singletons required with the provided <paramref name="implementationFactory"/> against the <paramref name="pipelineName"/>.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="T"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                       string pipelineName,
                                                                       Func<IServiceProvider, T> implementationFactory)
        where T : class
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNullOrEmpty(pipelineName);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton(pipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures <see cref="IMessageDestination"/> with <typeparamref name="TDestination"/> implementation for the <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TDestination : class, IMessageDestination
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageDestination>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TDestination>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures <see cref="IMessageDestination"/> with <typeparamref name="TDestination"/> implementation for the <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TDestination"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
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
    /// Configures <see cref="IMessageDestination"/> with <typeparamref name="TDestination"/> implementation for the <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="IMessageDestination"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TDestination"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                            string pipelineName,
                                                                                            Func<IServiceProvider, TDestination> implementationFactory)
        where TDestination : class, IMessageDestination
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNullOrEmpty(pipelineName);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageDestination>(pipelineName, implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures <see cref="IMessageSource"/> with <typeparamref name="TSource"/> implementation for the <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of <see cref="IMessageSource"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TSource : class, IMessageSource
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageSource, TSource>(pipelineBuilder.PipelineName);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures <see cref="IMessageSource"/> with <typeparamref name="TSource"/> implementation for the <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of <see cref="IMessageSource"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TSource"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
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
    /// Adds the <see cref="IMessageMiddleware"/> with <typeparamref name="TMiddleware"/> implementation to the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="IMessageMiddleware"/> in the pipeline is determined by the order of the calls to this method.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="IMessageMiddleware"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TMiddleware : class, IMessageMiddleware
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageMiddleware>(pipelineBuilder.PipelineName);
        return pipelineBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IMessageMiddleware"/> with <typeparamref name="TMiddleware"/> implementation to the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="IMessageMiddleware"/> in the pipeline is determined by the order of the calls to this method.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="IMessageMiddleware"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TMiddleware"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
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
    /// Configures the terminal <see cref="IMessageDelegate"/> with <typeparamref name="TDelegate"/> implementation to the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="IMessageMiddleware"/> in the pipeline via:
    ///  1. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder)"/> OR
    ///  2. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TMiddleware})"/>
    /// before calling this method.
    /// </remarks>
    /// <typeparam name="TDelegate">Type of <see cref="IMessageDelegate"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate<TDelegate>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TDelegate : class, IMessageDelegate
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<Func<IServiceProvider, IMessageDelegate>>(pipelineBuilder.PipelineName, sp => sp => sp.GetRequiredService<TDelegate>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the terminal <see cref="IMessageDelegate"/> with <typeparamref name="TDelegate"/> implementation to the <see cref="IMessageMiddleware"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="IMessageMiddleware"/> in the pipeline via:
    ///  1. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder)"/> OR
    ///  2. <see cref="AddMessageMiddleware{TMiddleware}(IAsyncProcessingPipelineBuilder, Func{IServiceProvider, TMiddleware})"/>
    /// before calling this method.
    /// </remarks>
    /// <typeparam name="TDelegate">Type of <see cref="IMessageDelegate"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TDelegate"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate<TDelegate>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                              Func<IServiceProvider, TDelegate> implementationFactory)
        where TDelegate : class, IMessageDelegate
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<Func<IServiceProvider, IMessageDelegate>>(pipelineBuilder.PipelineName, sp => implementationFactory);
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageConsumer"/> with <typeparamref name="TConsumer"/> implementation.
    /// </summary>
    /// <typeparam name="TConsumer">Type of <see cref="IMessageConsumer"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder)
        where TConsumer : class, IMessageConsumer
    {
        _ = Throw.IfNull(pipelineBuilder);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageConsumer>(pipelineBuilder.PipelineName, sp => sp.GetRequiredService<TConsumer>());
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the <see cref="IMessageConsumer"/> with <typeparamref name="TConsumer"/> implementation.
    /// </summary>
    /// <typeparam name="TConsumer">Type of <see cref="IMessageConsumer"/> implementation.</typeparam>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <param name="implementationFactory">Implementation for <typeparamref name="TConsumer"/>.</param>
    /// <returns><see cref="IAsyncProcessingPipelineBuilder"/> to chain additional calls.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder,
                                                                                      Func<IServiceProvider, TConsumer> implementationFactory)
        where TConsumer : class, IMessageConsumer
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = Throw.IfNull(implementationFactory);

        _ = pipelineBuilder.Services.AddNamedSingleton<IMessageConsumer>(pipelineBuilder.PipelineName, sp => implementationFactory(sp));
        return pipelineBuilder;
    }

    /// <summary>
    /// Configures the previously registered <see cref="IMessageConsumer"/> as a <see cref="BackgroundService"/>.
    /// </summary>
    /// <param name="pipelineBuilder"><see cref="IAsyncProcessingPipelineBuilder"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void RunConsumerAsBackgroundService(this IAsyncProcessingPipelineBuilder pipelineBuilder)
    {
        _ = Throw.IfNull(pipelineBuilder);
        _ = pipelineBuilder.Services.AddSingleton<IHostedService>(serviceProvider =>
        {
            INamedServiceProvider<IMessageConsumer> namedMessageConsumerProvider = serviceProvider.GetRequiredService<INamedServiceProvider<IMessageConsumer>>();
            IMessageConsumer messageConsumer = namedMessageConsumerProvider.GetRequiredService(pipelineBuilder.PipelineName);
            return new ConsumerBackgroundService(messageConsumer);
        });
    }
}
