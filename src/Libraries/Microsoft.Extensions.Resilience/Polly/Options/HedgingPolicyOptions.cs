// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Hedging policy options.
/// </summary>
public class HedgingPolicyOptions
{
    /// <summary>
    /// A <see cref="TimeSpan"/> that represents the infinite hedging delay.
    /// </summary>
    public static readonly TimeSpan InfiniteHedgingDelay = TimeSpan.FromMilliseconds(-1);

    private const int DefaultMaxHedgedAttempts = 2;
    private const int MinimumHedgedAttempts = 2;
    private const int MaximumHedgedAttempts = 10;
    private static readonly TimeSpan _defaultHedgingDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the minimal time of waiting before spawning a new hedged call.
    /// </summary>
    /// <remarks>
    /// Default is set to 2 seconds.
    ///
    /// You can also use <see cref="TimeSpan.Zero"/> to create all hedged tasks (value of <see cref="MaxHedgedAttempts"/>) at once
    /// or <see cref="InfiniteHedgingDelay"/> to force the hedging policy to never create new task before the old one is finished.
    ///
    /// If you want a greater control over hedging delay customization use <see cref="HedgingDelayGenerator"/>.
    /// </remarks>
    [TimeSpan(-1, Exclusive = false)]
    public TimeSpan HedgingDelay { get; set; } = _defaultHedgingDelay;

    /// <summary>
    /// Gets or sets the delegate that is used to customize the hedging delays after each hedging task is created.
    /// </summary>
    /// <remarks>
    /// The <see cref="HedgingDelayGenerator"/> takes precedence over <see cref="HedgingDelay"/>. If specified, the <see cref="HedgingDelay"/> is ignored.
    ///
    /// By default, this value is <c>null</c>.
    /// </remarks>
    public Func<HedgingDelayArguments, TimeSpan>? HedgingDelayGenerator { get; set; }

    /// <summary>
    /// Gets or sets the maximum hedged attempts to perform the desired task.
    /// </summary>
    /// <value>
    /// The number of concurrent hedged tasks that will be triggered by the policy. The default value is 2.
    /// </value>
    /// <remarks>
    /// This value includes the primary hedged task that is initially performed, and the further tasks that will
    /// be fetched from the provider and spawned in parallel.
    /// The value must be greater than or equal to 2, and less than or equal to 10.
    /// </remarks>
    [Range(MinimumHedgedAttempts, MaximumHedgedAttempts)]
    public int MaxHedgedAttempts { get; set; } = DefaultMaxHedgedAttempts;

    private Predicate<Exception> _shouldHandleException = _ => true;
    private Func<HedgingTaskArguments, Task> _onHedgingAsync = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the exception predicate to filter the type of exception the policy can handle.
    /// </summary>
    /// <remarks>
    /// By default any exception will be retried.
    /// </remarks>
    [Required]
    public Predicate<Exception> ShouldHandleException
    {
        get => _shouldHandleException;
        set => _shouldHandleException = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the action to call asynchronously before invoking the hedging delegate.
    /// </summary>
    [Required]
    public Func<HedgingTaskArguments, Task> OnHedgingAsync
    {
        get => _onHedgingAsync;
        set => _onHedgingAsync = Throw.IfNull(value);
    }
}
