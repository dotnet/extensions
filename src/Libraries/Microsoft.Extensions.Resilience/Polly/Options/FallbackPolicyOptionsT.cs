// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Options for the fallback policy.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
public class FallbackPolicyOptions<TResult> : FallbackPolicyOptions
{
    private Predicate<TResult> _shouldHandleResultAsError = _ => false;
    private Func<FallbackTaskArguments<TResult>, Task> _onFallbackAsync = _ => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the predicate to filter results the policy will handle.
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
    /// Gets or sets the action to call asynchronously before invoking the task performed in the fallback scenario,
    /// after the initially executed action encounters a transient failure.
    /// </summary>
    [Required]
    public new Func<FallbackTaskArguments<TResult>, Task> OnFallbackAsync
    {
        get => _onFallbackAsync;
        set => _onFallbackAsync = Throw.IfNull(value);
    }
}
