// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface to build a <see cref="IMessageConsumer"/> and associate <see cref="IMessageMiddleware"/> for the same.
/// </summary>
public interface IAsyncProcessingPipelineBuilder
{
    /// <summary>
    /// Gets the name of the message pipeline.
    /// </summary>
    public string PipelineName { get; }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; }
}
