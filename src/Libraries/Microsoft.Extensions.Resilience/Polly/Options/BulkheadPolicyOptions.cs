// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Options for the bulkhead policy.
/// </summary>
public class BulkheadPolicyOptions
{
    private const int DefaultMaxLimitActions = 10000;
    private const int DefaultMaxQueuedActions = 0;
    private const int DefaultMaxConcurrency = 1000;

    /// <summary>
    /// Gets or sets the maximum parallelization of executions through the bulkhead.
    /// </summary>
    /// <value>
    /// The default value is 1000.
    /// </value>
    [Range(1, DefaultMaxLimitActions)]
    public int MaxConcurrency { get; set; } = DefaultMaxConcurrency;

    /// <summary>
    /// Gets or sets the maximum number of actions that may be queued (waiting to acquire an execution slot) at any one time.
    /// </summary>
    /// <value>
    /// The default value is 0.
    /// </value>
    [Range(0, DefaultMaxLimitActions)]
    public int MaxQueuedActions { get; set; } = DefaultMaxQueuedActions;

    private Func<BulkheadTaskArguments, Task> _onBulkheadRejectedAsync = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the action performed during the bulkhead rejection of the bulkhead policy.
    /// </summary>
    [Required]
    public Func<BulkheadTaskArguments, Task> OnBulkheadRejectedAsync
    {
        get => _onBulkheadRejectedAsync;
        set => _onBulkheadRejectedAsync = Throw.IfNull(value);
    }
}
