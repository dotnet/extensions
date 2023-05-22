// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Circuit breaker policy options.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
#pragma warning disable SA1649 // File name should match first type name
public class CircuitBreakerPolicyOptions<TResult> : CircuitBreakerPolicyOptions
#pragma warning restore SA1649 // File name should match first type name
{
    private Predicate<TResult> _shouldHandleResultAsError = _ => false;
    private Action<BreakActionArguments<TResult>> _onCircuitBreak = _ => { };

    /// <summary>
    /// Gets or sets the predicate which defines the results which are treated as transient errors.
    /// </summary>
    /// <remarks>
    /// By default, it will not retry any final result.
    /// </remarks>
    public Predicate<TResult> ShouldHandleResultAsError
    {
        get => _shouldHandleResultAsError;
        set => _shouldHandleResultAsError = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the action performed when the circuit breaker breaks.
    /// </summary>
    [Required]
    public new Action<BreakActionArguments<TResult>> OnCircuitBreak
    {
        get => _onCircuitBreak;
        set => _onCircuitBreak = Throw.IfNull(value);
    }
}
