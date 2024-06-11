// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
#endif

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for the standard HTTP resilience pipeline.
/// </summary>
public interface IHttpStandardResiliencePipelineBuilder
{
    /// <summary>
    /// Gets the name of the resilience pipeline configured by this builder.
    /// </summary>
    string PipelineName { get; }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    IServiceCollection Services { get; }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Gets the HTTP client builder.
    /// </summary>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
    IHttpClientBuilder HttpClientBuilder => null!;
#endif
}
