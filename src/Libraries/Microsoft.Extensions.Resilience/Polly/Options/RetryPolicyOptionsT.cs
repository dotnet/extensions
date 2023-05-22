// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Retry policy options.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
public class RetryPolicyOptions<TResult> : RetryPolicyOptions
{
    internal static readonly Func<RetryActionArguments<TResult>, Task> DefaultOnRetryAsync = _ => Task.CompletedTask;

    private Predicate<TResult> _shouldHandleResultAsError = _ => false;
    private Func<RetryActionArguments<TResult>, Task> _onRetryAsync = DefaultOnRetryAsync;

    /// <summary>
    /// Gets or sets the predicate which defines what results shall be
    /// treated as transient error by the policy.
    /// </summary>
    /// <remarks>
    /// By default, it will not retry any final result.
    /// </remarks>
    [Required]
    public Predicate<TResult> ShouldHandleResultAsError
    {
        get => _shouldHandleResultAsError;
        set => _shouldHandleResultAsError = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the action performed during the retry attempt of the retry policy.
    /// </summary>
    [Required]
    public new Func<RetryActionArguments<TResult>, Task> OnRetryAsync
    {
        get => _onRetryAsync;
        set => _onRetryAsync = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the delegate for customizing delay for the retry policy.
    /// </summary>
    /// <remarks>
    /// By default this is null and the delay will be calculated based on the backoff type only.
    /// </remarks>
    public Func<RetryDelayArguments<TResult>, TimeSpan>? RetryDelayGenerator { get; set; }
}
