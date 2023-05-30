// Assembly 'System.Cloud.Messaging'

using Microsoft.Extensions.DependencyInjection;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to create async processing pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Create an async processing pipeline with the provided <paramref name="pipelineName" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pipelineName">The name of the async processing pipeline.</param>
    /// <returns>The builder for async processing pipeline.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static IAsyncProcessingPipelineBuilder AddAsyncPipeline(this IServiceCollection services, string pipelineName);
}
