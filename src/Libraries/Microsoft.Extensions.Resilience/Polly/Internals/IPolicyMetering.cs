// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Metering support for generic and non-generic policies.
/// </summary>
internal interface IPolicyMetering
{
    /// <summary>
    /// Initializes the instance.
    /// </summary>
    /// <param name="pipelineId">The pipeline id.</param>
    void Initialize(PipelineId pipelineId);

    /// <summary>
    /// Records the policy event.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="fault">The fault instance.</param>
    /// <param name="context">The context associated with the event.</param>
    void RecordEvent(string policyName, string eventName, Exception? fault, Context? context);

    /// <summary>
    /// Records the policy event.
    /// </summary>
    /// <typeparam name="TResult">The type of result.</typeparam>
    /// <param name="policyName">The policy name.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="fault">The fault instance.</param>
    /// <param name="context">The context associated with the event.</param>
    void RecordEvent<TResult>(string policyName, string eventName, DelegateResult<TResult>? fault, Context? context);
}
