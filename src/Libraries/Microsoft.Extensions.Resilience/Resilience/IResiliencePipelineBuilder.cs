// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// The builder for configuring the policy pipeline.
/// </summary>
/// <typeparam name="TResult">The type of the result this pipeline handles.</typeparam>
public interface IResiliencePipelineBuilder<TResult>
{
    /// <summary>
    /// Gets the name of the pipeline configured by this builder.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    IServiceCollection Services { get; }
}

