// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable S109 // Magic numbers should not be used

internal static partial class Log
{
    [LogMethod(0, LogLevel.Debug, "Executing pipeline. Pipeline Name: {pipelineName}, Pipeline Key: {pipelineKey}")]
    public static partial void ExecutingPipeline(this ILogger logger, string pipelineName, string pipelineKey);

    [LogMethod(1, LogLevel.Debug, "Pipeline executed in {elapsed}ms. Pipeline Name: {pipelineName}, Pipeline Key: {pipelineKey}")]
    public static partial void PipelineExecuted(this ILogger logger, string pipelineName, string pipelineKey, long elapsed);

    [LogMethod(2, LogLevel.Warning, "Pipeline execution failed in {elapsed}ms. Pipeline Name: {pipelineName}, Pipeline Key: {pipelineKey}")]
    public static partial void PipelineFailed(this ILogger logger, Exception error, string pipelineName, string pipelineKey, long elapsed);

    [LogMethod(3, LogLevel.Debug, "Pipeline {pipelineName} has been updated.")]
    public static partial void PipelineUpdated(this ILogger logger, string pipelineName);

    [LogMethod(4, LogLevel.Warning, "Pipeline update failed. Pipeline Name: {pipelineName}.")]
    public static partial void PipelineUpdatedFailure(this ILogger logger, string pipelineName, Exception exception);

    [LogMethod(5, LogLevel.Debug, "Configuration for policy {policyName} has been updated.")]
    public static partial void PolicyInPipelineUpdated(this ILogger logger, string policyName);

}
