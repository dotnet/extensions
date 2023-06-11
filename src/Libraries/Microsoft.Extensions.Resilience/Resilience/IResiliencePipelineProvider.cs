// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Polly;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// The resilience pipeline provider creates and caches pipeline instances that are configured using <see cref="IResiliencePipelineBuilder{TResult}"/>.
/// </summary>
/// <seealso cref="IResiliencePipelineBuilder{TResult}"/>
/// <seealso cref="IAsyncPolicy{TResult}"/>
/// <seealso cref="IAsyncPolicy"/>
/// <remarks>
/// Use this interface to create instances of both generic and non-generic resilience pipelines.
/// </remarks>
public interface IResiliencePipelineProvider
{
    /// <summary>Gets the pipeline instance.</summary>
    /// <param name="pipelineName">A pipeline name.</param>
    /// <returns>The pipeline instance.</returns>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <exception cref="ArgumentException"><paramref name="pipelineName"/> is an empty string.</exception>
    /// <exception cref="OptionsValidationException">The pipeline identified by <paramref name="pipelineName"/> is invalid or not configured.</exception>
    /// <remarks>
    /// Make sure that the pipeline identified by <paramref name="pipelineName"/> is configured, otherwise the provider won't be able to create
    /// it and throws an exception.
    /// </remarks>
    public IAsyncPolicy<TResult> GetPipeline<TResult>(string pipelineName);

    /// <summary>Gets a <paramref name="pipelineName"/> pipeline instance cached by <paramref name="pipelineKey"/>. If the target pipeline is not cached yet,
    /// the provider creates and caches it and then returns the instance.</summary>
    /// <param name="pipelineName">A pipeline name.</param>
    /// <param name="pipelineKey">The pipeline key associated with a cached instance of a <paramref name="pipelineName"/> pipeline.</param>
    /// <returns>The pipeline instance.</returns>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <exception cref="ArgumentException"><paramref name="pipelineName"/> is an empty string.</exception>
    /// <exception cref="OptionsValidationException">The pipeline identified by <paramref name="pipelineName"/> is invalid or not configured.</exception>
    /// <remarks>
    /// This method enables to have multiple instances of the same <paramref name="pipelineName"/> pipeline that are cached by the <paramref name="pipelineKey"/>.
    /// Make sure that the pipeline identified by <paramref name="pipelineName"/> is configured, otherwise the provider won't be able to create
    /// it and throws an exception.
    /// </remarks>
    public IAsyncPolicy<TResult> GetPipeline<TResult>(string pipelineName, string pipelineKey);
}
