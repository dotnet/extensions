// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Composite key for the pipeline.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
internal sealed record PipelineId(string PipelineName, string PipelineKey, string? ResultType, string PolicyPipelineKey)
{
    /// <summary>
    /// Creates a pipeline id.
    /// </summary>
    /// <typeparam name="T">The type of result the pipeline handles.</typeparam>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="pipelineKey">The pipeline key.</param>
    /// <returns>The pipeline id instance.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static PipelineId Create<T>(string pipelineName, string pipelineKey)
    {
        var policyPipelineKey = string.IsNullOrEmpty(pipelineKey) ? $"{typeof(T).Name}-{pipelineName}" : $"{typeof(T).Name}-{pipelineName}-{pipelineKey}";

        return new PipelineId(Throw.IfNullOrEmpty(pipelineName), pipelineKey, typeof(T).Name, policyPipelineKey);
    }

    /// <summary>
    /// Creates a pipeline id.
    /// </summary>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="pipelineKey">The pipeline key.</param>
    /// <returns>The pipeline id instance.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static PipelineId Create(string pipelineName, string pipelineKey)
    {
        var policyPipelineKey = string.IsNullOrEmpty(pipelineKey) ? pipelineName : $"{pipelineName}-{pipelineKey}";

        return new PipelineId(Throw.IfNullOrEmpty(pipelineName), pipelineKey, null, policyPipelineKey);
    }
}
