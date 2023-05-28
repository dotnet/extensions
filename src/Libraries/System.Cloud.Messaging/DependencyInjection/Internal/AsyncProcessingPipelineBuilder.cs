// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Internal;

/// <summary>
/// Implementation of <see cref="IAsyncProcessingPipelineBuilder"/>.
/// </summary>
internal sealed class AsyncProcessingPipelineBuilder : IAsyncProcessingPipelineBuilder
{
    /// <inheritdoc/>
    public string PipelineName { get; }

    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncProcessingPipelineBuilder"/> class.
    /// </summary>
    public AsyncProcessingPipelineBuilder(string pipelineName, IServiceCollection services)
    {
        PipelineName = Throw.IfNullOrEmpty(pipelineName);
        Services = Throw.IfNull(services);
    }
}
