// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Metering support for pipelines.
/// </summary>
internal interface IPipelineMetering
{
    /// <summary>
    /// Initializes the instance.
    /// </summary>
    /// <param name="pipelineId">The pipeline id.</param>
    void Initialize(PipelineId pipelineId);

    /// <summary>
    /// Records the pipeline execution.
    /// </summary>
    /// <param name="executionTimeInMs">The pipeline execution time.</param>
    /// <param name="fault">The fault instance.</param>
    /// <param name="context">The context associated with the event.</param>
    void RecordPipelineExecution(long executionTimeInMs, Exception? fault, Context context);
}
