// Assembly 'System.Cloud.Messaging'

using Microsoft.Extensions.DependencyInjection;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface to register services for the async processing pipeline.
/// </summary>
public interface IAsyncProcessingPipelineBuilder
{
    /// <summary>
    /// Gets the name of the message pipeline.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Gets the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    IServiceCollection Services { get; }
}
