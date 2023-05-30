// Assembly 'System.Cloud.Messaging'

namespace System.Cloud.Messaging;

public static class AsyncProcessingPipelineBuilderExtensions
{
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where T : class;
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory) where T : class;
    public static IAsyncProcessingPipelineBuilder AddNamedSingleton<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, string name, Func<IServiceProvider, T> implementationFactory) where T : class;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TDestination : class, IMessageDestination;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TDestination> implementationFactory) where TDestination : class, IMessageDestination;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageDestination<TDestination>(this IAsyncProcessingPipelineBuilder pipelineBuilder, string name, Func<IServiceProvider, TDestination> implementationFactory) where TDestination : class, IMessageDestination;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TSource : class, IMessageSource;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageSource<TSource>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TSource> implementationFactory) where TSource : class, IMessageSource;
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TMiddleware : class, IMessageMiddleware;
    public static IAsyncProcessingPipelineBuilder AddMessageMiddleware<TMiddleware>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TMiddleware> implementationFactory) where TMiddleware : class, IMessageMiddleware;
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder);
    public static IAsyncProcessingPipelineBuilder ConfigureTerminalMessageDelegate(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, MessageDelegate> implementationFactory);
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder) where TConsumer : MessageConsumer;
    public static IAsyncProcessingPipelineBuilder ConfigureMessageConsumer<TConsumer>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, TConsumer> implementationFactory) where TConsumer : MessageConsumer;
    public static void RunConsumerAsBackgroundService(this IAsyncProcessingPipelineBuilder pipelineBuilder);
}
