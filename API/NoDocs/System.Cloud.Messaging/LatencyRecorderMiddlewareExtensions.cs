// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Latency;

namespace System.Cloud.Messaging;

public static class LatencyRecorderMiddlewareExtensions
{
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder);
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory, Func<IServiceProvider, IEnumerable<ILatencyDataExporter>> exporterFactory) where T : class, ILatencyContextProvider;
    public static IAsyncProcessingPipelineBuilder AddLatencyContextMiddleware<T>(this IAsyncProcessingPipelineBuilder pipelineBuilder, Func<IServiceProvider, T> implementationFactory) where T : class, ILatencyContext;
    public static IAsyncProcessingPipelineBuilder AddLatencyRecorderMessageMiddleware(this IAsyncProcessingPipelineBuilder pipelineBuilder, MeasureToken successMeasureToken, MeasureToken failureMeasureToken);
}
