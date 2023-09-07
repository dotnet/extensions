// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for configuring the HTTP client resilience pipeline.
/// </summary>
public interface IHttpResiliencePipelineBuilder
{
    /// <summary>
    /// Gets the name of the resilience pipeline configured by this builder.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    IServiceCollection Services { get; }
}
