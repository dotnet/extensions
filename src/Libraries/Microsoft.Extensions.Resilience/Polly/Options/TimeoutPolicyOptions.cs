// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.Diagnostics;
using Polly.Timeout;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Options for the timeout policy.
/// </summary>
public class TimeoutPolicyOptions
{
    private static readonly Func<TimeoutTaskArguments, Task> _defaultOnTimedOutAsync = _ => Task.CompletedTask;
    private static readonly TimeSpan _defaultTimeoutInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout interval.
    /// </summary>
    /// <value>
    /// The default value is 30 seconds.
    /// </value>
    [TimeSpan(0, Exclusive = true)]
    public TimeSpan TimeoutInterval { get; set; } = _defaultTimeoutInterval;

    /// <summary>
    /// Gets or sets the timeout strategy.
    /// </summary>
    /// <remarks>
    /// Default is set to Optimistic Timeout strategy:
    /// <see href="https://github.com/App-vNext/Polly/wiki/Timeout#optimistic-timeout"/>.
    /// </remarks>
    public TimeoutStrategy TimeoutStrategy { get; set; } = TimeoutStrategy.Optimistic;

    private Func<TimeoutTaskArguments, Task> _onTimedOutAsync = _defaultOnTimedOutAsync;

    /// <summary>
    /// Gets or sets the action performed during the timeout attempt of the timeout policy.
    /// </summary>
    [Required]
    public Func<TimeoutTaskArguments, Task> OnTimedOutAsync
    {
        get => _onTimedOutAsync;
        set => _onTimedOutAsync = Throw.IfNull(value);
    }
}
