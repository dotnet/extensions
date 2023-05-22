// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Options for the fallback policy.
/// </summary>
public class FallbackPolicyOptions
{
    private Func<FallbackTaskArguments, Task> _onFallbackAsync = _ => Task.CompletedTask;

    private Predicate<Exception> _shouldHandleException = _ => true;

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
    /// Gets or sets the action to call asynchronously before invoking the task performed in the fallback scenario,
    /// after the initially executed action encounters a transient failure.
    /// </summary>
    [Required]
    public Func<FallbackTaskArguments, Task> OnFallbackAsync
    {
        get => _onFallbackAsync;
        set => _onFallbackAsync = Throw.IfNull(value);
    }
}
