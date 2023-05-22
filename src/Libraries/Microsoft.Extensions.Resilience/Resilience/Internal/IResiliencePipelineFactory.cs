// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Encapsulates the logic for creation of resilience pipelines.
/// </summary>
/// <remarks>
/// Only allows creation of pipelines that were previously added and configured
/// by using the <see cref="ServiceCollectionExtensions.AddResiliencePipeline{TPolicyResult}(Microsoft.Extensions.DependencyInjection.IServiceCollection, string)"/> method.
/// </remarks>
internal interface IResiliencePipelineFactory
{
    /// <summary>
    /// Creates a new instance of resilience pipeline.
    /// </summary>
    /// <typeparam name="TPolicyResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="pipelineName">The pipeline name.</param>
    /// <param name="pipelineKey">The pipeline key.</param>
    /// <returns>The policy pipeline.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="pipelineName"/> is null or empty string.</exception>
    /// <exception cref="InvalidOperationException">The pipeline identified by <paramref name="pipelineName"/> is not recognized.</exception>
    IAsyncPolicy<TPolicyResult> CreatePipeline<TPolicyResult>(string pipelineName, string pipelineKey = "");
}
