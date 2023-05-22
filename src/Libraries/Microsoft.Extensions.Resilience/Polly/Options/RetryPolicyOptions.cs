// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Retry policy options.
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Magic value representing infinite retries.
    /// </summary>
    public const int InfiniteRetry = -1;

    /// <summary>
    /// Maximal allowed retry counts unless infinite.
    /// </summary>
    internal const int MaxRetryCount = 100;

    /// <summary>
    /// Maximal allowed BaseDelay (1 day).
    /// </summary>
    internal const int MaxBaseDelay = 24 * 3600 * 1000;

    private const int DefaultRetryCount = 3;
    private const BackoffType DefaultBackoffType = BackoffType.ExponentialWithJitter;
    private static readonly TimeSpan _defaultBackoffBasedDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the maximum number of retries to use, in addition to the original call.
    /// </summary>
    /// <remarks>
    /// For infinite retries use <c>InfiniteRetry</c> (-1).
    /// </remarks>
    [Range(InfiniteRetry, MaxRetryCount)]
    public int RetryCount { get; set; } = DefaultRetryCount;

    /// <summary>
    /// Gets or sets the type of the back-off.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="BackoffType.ExponentialWithJitter"/>.
    /// </remarks>
    public BackoffType BackoffType { get; set; } = DefaultBackoffType;

    /// <summary>
    /// Gets or sets the delay between retries based on the backoff type, <see cref="Options.BackoffType"/>.
    /// </summary>
    /// <remarks>
    /// Default set to 2 seconds.
    /// For <see cref="BackoffType.ExponentialWithJitter"/> this represents the median delay to target before the first retry.
    /// For the <see cref="BackoffType.Linear"/> it represents the initial delay, the following delays increasing linearly with this value.
    /// In case of <see cref="BackoffType.Constant"/> it represents the constant delay between retries.
    /// </remarks>
    [TimeSpan(0, MaxBaseDelay)]
    public TimeSpan BaseDelay { get; set; } = _defaultBackoffBasedDelay;

    private Predicate<Exception> _shouldHandleException = _ => true;
    private Func<RetryActionArguments, Task> _onRetryAsync = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the predicate which filters the type of exception the policy can handle.
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
    /// Gets or sets the action performed during the retry attempt of the retry policy.
    /// </summary>
    [Required]
    public Func<RetryActionArguments, Task> OnRetryAsync
    {
        get => _onRetryAsync;
        set => _onRetryAsync = Throw.IfNull(value);
    }
}
