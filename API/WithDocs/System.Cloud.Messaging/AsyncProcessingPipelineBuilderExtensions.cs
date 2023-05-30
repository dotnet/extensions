// Assembly 'System.Cloud.Messaging'

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to register:
///   1. singletons,
///   2. <see cref="T:System.Cloud.Messaging.IMessageSource" />,
///   3. <see cref="T:System.Cloud.Messaging.IMessageDestination" />,
///   4. <see cref="T:System.Cloud.Messaging.IMessageMiddleware" />,
///   5. <see cref="T:System.Cloud.Messaging.MessageDelegate" />,
///   6. <see cref="T:System.Cloud.Messaging.MessageConsumer" />.
/// </summary>
public static class AsyncProcessingPipelineBuilderExtensions
{
    /// <summary>
    /// Adds any singletons required for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the singleton <typeparamref name="T" /> is already registered with the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </remarks>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where T : class;

    /// <summary>
    /// Adds any singletons required for the async processing pipeline with the provided <paramref name="implementationFactory" />.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for the singleton type.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory) where T : class;

    /// <summary>
    /// Adds any singletons required for the async processing pipeline with the provided <paramref name="implementationFactory" /> against the provided <paramref name="name" />.
    /// </summary>
    /// <typeparam name="T">Type of singleton.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="name">The name with which the singleton is registered.</param>
    /// <param name="implementationFactory">The implementation factory for the singleton type.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, string name, Func<IServiceProvider, T> implementationFactory) where T : class;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TDestination" /> is already registered with the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </remarks>
    /// <typeparam name="TDestination">Type of <see cref="T:System.Cloud.Messaging.IMessageDestination" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TDestination : class, IMessageDestination;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="T:System.Cloud.Messaging.IMessageDestination" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.IMessageDestination" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TDestination> implementationFactory) where TDestination : class, IMessageDestination;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> for the async processing pipeline with the provided name and implementation factory.
    /// </summary>
    /// <typeparam name="TDestination">Type of <see cref="T:System.Cloud.Messaging.IMessageDestination" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="name">The name with which the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> is registered with.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.IMessageDestination" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder, string name, Func<IServiceProvider, TDestination> implementationFactory) where TDestination : class, IMessageDestination;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.IMessageSource" /> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TSource" /> is already registered with the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </remarks>
    /// <typeparam name="TSource">Type of <see cref="T:System.Cloud.Messaging.IMessageSource" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TSource : class, IMessageSource;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.IMessageSource" /> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TSource">Type of <see cref="T:System.Cloud.Messaging.IMessageSource" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.IMessageSource" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TSource> implementationFactory) where TSource : class, IMessageSource;

    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> in the pipeline is determined by the order of the calls to this method.
    /// Ensure the <typeparamref name="TMiddleware" /> is already registered with the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TMiddleware : class, IMessageMiddleware;

    /// <summary>
    /// Adds the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <remarks>
    /// Ordering of the <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> in the pipeline is determined by the order of the calls to this method.
    /// </remarks>
    /// <typeparam name="TMiddleware">Type of <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.IMessageMiddleware" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TMiddleware> implementationFactory) where TMiddleware : class, IMessageMiddleware;

    /// <summary>
    /// Configures the terminal <see cref="T:System.Cloud.Messaging.MessageDelegate" /> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> in the pipeline before calling this method via:
    ///  1. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder)" /> OR
    ///  2. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" />.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder);

    /// <summary>
    /// Configures the terminal <see cref="T:System.Cloud.Messaging.MessageDelegate" /> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <remarks>
    /// Ensure to add the required <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> in the pipeline before calling this method via:
    ///  1. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder)" /> OR
    ///  2. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.AddMessageMiddleware``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" />.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.MessageDelegate" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, MessageDelegate> implementationFactory);

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.MessageConsumer" /> for the async processing pipeline.
    /// </summary>
    /// <remarks>
    /// Ensure the <typeparamref name="TConsumer" /> is already registered with the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </remarks>
    /// <typeparam name="TConsumer">Type of <see cref="T:System.Cloud.Messaging.MessageConsumer" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TConsumer : MessageConsumer;

    /// <summary>
    /// Configures the <see cref="T:System.Cloud.Messaging.MessageConsumer" /> for the async processing pipeline with the provided implementation factory.
    /// </summary>
    /// <typeparam name="TConsumer">Type of <see cref="T:System.Cloud.Messaging.MessageConsumer" /> implementation.</typeparam>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <param name="implementationFactory">The implementation factory for <see cref="T:System.Cloud.Messaging.MessageConsumer" />.</param>
    /// <returns><see cref="T:System.Cloud.Messaging.IAsyncProcessingPipelineBuilder" /> to chain additional calls.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TConsumer> implementationFactory) where TConsumer : MessageConsumer;

    /// <summary>
    /// Configures the previously registered <see cref="T:System.Cloud.Messaging.MessageConsumer" /> for the async processing pipeline as a <see cref="T:Microsoft.Extensions.Hosting.BackgroundService" />.
    /// </summary>
    /// <remarks>
    /// Ensure to configure the required <see cref="T:System.Cloud.Messaging.MessageConsumer" /> before calling this method via:
    ///   1. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.ConfigureMessageConsumer``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder)" /> OR
    ///   2. <see cref="M:System.Cloud.Messaging.AsyncProcessingPipelineBuilderExtensions.ConfigureMessageConsumer``1(System.Cloud.Messaging.IAsyncProcessingPipelineBuilder,System.Func{System.IServiceProvider,``0})" />.
    /// </remarks>
    /// <param name="pipelineBuilder">The builder for async processing pipeline.</param>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static void RunConsumerAsBackgroundService(this IAsyncProcessingPipelineBuilder pipelineBuilder);
}
