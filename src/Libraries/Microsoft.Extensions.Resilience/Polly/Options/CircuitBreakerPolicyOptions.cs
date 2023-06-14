// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Circuit breaker policy options.
/// </summary>
public class CircuitBreakerPolicyOptions
{
    private const double DefaultFailureThreshold = 0.1;
    private const int DefaultMinimumThroughput = 100;
    private const int MinPolicyWaitingMilliseconds = 500;
    private static readonly TimeSpan _defaultBreakDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _defaultSamplingDuration = TimeSpan.FromSeconds(30);
    private Action<BreakActionArguments> _onCircuitBreak = _ => { };
    private Predicate<Exception> _shouldHandleException = _ => true;
    private Action<ResetActionArguments> _onCircuitReset = (_) => { };

    /// <summary>
    /// Gets or sets the failure threshold.
    /// </summary>
    /// <value>
    /// A ratio number higher than 0, up to 1. The default value is 0.1.
    /// </value>
    /// <remarks>
    /// If the ratio of the number of failed requests to total requests exceeds this threshold, the circuit will break.
    /// </remarks>
    [ExclusiveRange(0, 1.0)]
    public double FailureThreshold { get; set; } = DefaultFailureThreshold;

    /// <summary>
    /// Gets or sets the minimum throughput.
    /// </summary>
    /// <value>
    /// The default value is 100.
    /// </value>
    /// <remarks>
    /// This defines how many actions must pass through the circuit in the time-slice,
    /// for statistics to be considered significant and the circuit-breaker to come into action.
    /// The value must be greater than 1.
    /// </remarks>
    [ExclusiveRange(1, int.MaxValue)]
    public int MinimumThroughput { get; set; } = DefaultMinimumThroughput;

    /// <summary>
    /// Gets or sets the duration of break.
    /// </summary>
    /// <value>
    /// The duration the circuit will stay open before resetting. The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// The value must be greater than 0.5 seconds.
    /// </remarks>
    [TimeSpan(MinPolicyWaitingMilliseconds, Exclusive = true)]
    public TimeSpan BreakDuration { get; set; } = _defaultBreakDuration;

    /// <summary>
    /// Gets or sets the duration of the sampling.
    /// </summary>
    /// <value>
    /// The duration of the time-slice over which failure ratios are assessed.
    /// The value must be greater than 0.5 seconds.
    /// The default value is 30 seconds.
    /// </value>
    [TimeSpan(MinPolicyWaitingMilliseconds, Exclusive = true)]
    public TimeSpan SamplingDuration { get; set; } = _defaultSamplingDuration;

    /// <summary>
    /// Gets or sets the predicate which filters the type of exception the policy can handle.
    /// </summary>
    /// <remarks>
    /// By default any exception will be retried.
    /// </remarks>
    public Predicate<Exception> ShouldHandleException
    {
        get => _shouldHandleException;
        set => _shouldHandleException = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the action performed when the circuit breaker resets itself.
    /// </summary>
    [Required]
    public Action<ResetActionArguments> OnCircuitReset
    {
        get => _onCircuitReset;
        set => _onCircuitReset = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the action performed when the circuit breaker breaks.
    /// </summary>
    [Required]
    public Action<BreakActionArguments> OnCircuitBreak
    {
        get => _onCircuitBreak;
        set => _onCircuitBreak = Throw.IfNull(value);
    }
}
