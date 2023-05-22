// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;
using Polly;
using Polly.Retry;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable CS0618 // access obsoleted members
#pragma warning disable R9A061

/// <summary>
/// Factory class for policy creation.
/// </summary>
internal sealed class PolicyFactory : IPolicyFactory
{
    private static readonly CircuitBreakerPolicyOptionsValidator _circuitBreakerOptionsValidator = new();
    private static readonly RetryPolicyOptionsValidator _retryOptionsValidator = new();
    private static readonly RetryPolicyOptionsCustomValidator _retryOptionsCustomValidator = new();
    private static readonly HedgingPolicyOptionsValidator _hedgingOptionsValidator = new();
    private static readonly TimeoutPolicyOptionsValidator _timeoutOptionsValidator = new();
    private static readonly BulkheadPolicyOptionsValidator _bulkheadOptionsValidator = new();

    private readonly ILogger _logger;
    private readonly IPolicyMetering _metering;
    private readonly Action<string, string, Context> _recordMetric;

    public PolicyFactory(ILogger<IPolicyFactory> logger, IPolicyMetering metering)
    {
        _logger = logger;
        _metering = metering;
        _recordMetric = RecordMetric;
    }

    public void Initialize(PipelineId pipelineId) => _metering.Initialize(pipelineId);

    /// <remarks>
    /// Reacts on proportion of failures (i.e. failureThreshold) by measuring the data within a custom interval (i.e. sampling duration)
    /// Imposes a minimal time interval before acting (i.e. minimumThroughput) and configurable break duration.
    /// <seealso href="http://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker" />
    /// </remarks>
    public IAsyncPolicy CreateCircuitBreakerPolicy(
        string policyName,
        CircuitBreakerPolicyOptions options)
    {
        _ = Throw.IfNull(options);

        PolicyFactoryUtility.ValidateOptions(_circuitBreakerOptionsValidator, options);

        return GetPolicyBuilderWithErrorHandling(options.ShouldHandleException)
                .AdvancedCircuitBreakerAsync(
                    options.FailureThreshold,
                    options.SamplingDuration,
                    options.MinimumThroughput,
                    options.BreakDuration,
                    (triggerEvent, duration, context) =>
                    {
                        var reason = FailureReasonResolver.GetFailureFromException(triggerEvent);
                        Log.LogCircuitBreak(_logger, policyName, duration.TotalSeconds, reason);
                        RecordMetric(PolicyEvents.CircuitBreakerOnBreakPolicyEvent, policyName, triggerEvent, context);

                        options.OnCircuitBreak(new BreakActionArguments(triggerEvent, context, duration, CancellationToken.None));
                    },
                    PolicyFactoryUtility.OnCircuitReset(policyName, options, PolicyEvents.CircuitBreakerOnResetPolicyEvent, _logger, _recordMetric),
                    () =>
                    {
                        Log.LogCircuitHalfOpen(_logger, policyName);
                        _metering.RecordEvent(policyName, PolicyEvents.CircuitBreakerOnHalfOpenPolicyEvent, fault: null, context: null);
                    });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Reacts on proportion of failures (i.e. failureThreshold) by measuring the data within a custom interval (i.e. sampling duration)
    /// Imposes a minimal time interval before acting (i.e. minimumThroughput) and configurable break duration.
    /// <seealso href="http://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker" />
    /// </remarks>
    public IAsyncPolicy<TResult> CreateCircuitBreakerPolicy<TResult>(
        string policyName,
        CircuitBreakerPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        ValidateOptions(_circuitBreakerOptionsValidator, options);

        var onCircuitReset = PolicyFactoryUtility.OnCircuitReset(
            policyName,
            options,
            PolicyEvents.CircuitBreakerOnResetPolicyEvent,
            _logger,
            _recordMetric);

        return GetPolicyBuilderWithErrorHandling(options.ShouldHandleResultAsError, options.ShouldHandleException)
                .AdvancedCircuitBreakerAsync(
                    options.FailureThreshold,
                    options.SamplingDuration,
                    options.MinimumThroughput,
                    options.BreakDuration,
                    (triggerEvent, duration, context) =>
                    {
                        var reason = FailureReasonResolver.GetFailureReason(triggerEvent);
                        Log.LogCircuitBreak(_logger, policyName, duration.TotalSeconds, reason);
                        RecordMetric(PolicyEvents.CircuitBreakerOnBreakPolicyEvent, policyName, triggerEvent, context);
                        options.OnCircuitBreak(new BreakActionArguments<TResult>(triggerEvent, context, duration, CancellationToken.None));
                    },
                    (context) => onCircuitReset(context),
                    () =>
                    {
                        Log.LogCircuitHalfOpen(_logger, policyName);
                        _metering.RecordEvent(policyName, PolicyEvents.CircuitBreakerOnHalfOpenPolicyEvent, fault: null, context: null);
                    });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Timing-wise, the onFallbackAsync runs the statement before the fallbackAction.
    /// The onFallbackAsync might use the initial result (i.e. before the fallbackAction is performed).
    /// Therefore the result should be disposed after the onFallbackAsync, before the fallbackAction.
    /// <see href="https://github.com/App-vNext/Polly/blob/2ac94cff75d2a63200dfab76f90e7c462f463a3b/src/Polly.Shared/Fallback/FallbackEngineAsync.cs#L54-L56" />.
    /// <see href="https://github.com/App-vNext/Polly/wiki/Fallback" />.
    /// </remarks>
    public IAsyncPolicy CreateFallbackPolicy(
        string policyName,
        FallbackScenarioTaskProvider fallbackScenarioTaskProvider,
        FallbackPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(fallbackScenarioTaskProvider);
        _ = Throw.IfNull(options);

        return
            GetPolicyBuilderWithErrorHandling(options.ShouldHandleException)
           .FallbackAsync(
                (_, context, ct) =>
                {
                    return fallbackScenarioTaskProvider(new FallbackScenarioTaskArguments(context, ct));
                },
                (fault, context) =>
                {
                    var reason = FailureReasonResolver.GetFailureFromException(fault);
                    Log.LogFallback(_logger, policyName, reason);
                    RecordMetric(PolicyEvents.FallbackPolicyEvent, policyName, fault, context);
                    return options.OnFallbackAsync(new FallbackTaskArguments(fault, context, CancellationToken.None));
                });
    }

    public IAsyncPolicy<TResult> CreateFallbackPolicy<TResult>(
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        FallbackPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(options);

        return
            GetPolicyBuilderWithErrorHandling(
                options.ShouldHandleResultAsError,
                options.ShouldHandleException)
           .FallbackAsync(
                (initialResult, context, ct) =>
                {
                    DisposeResult(initialResult);
                    return provider(new FallbackScenarioTaskArguments(context, ct));
                },
                (result, context) =>
                {
                    var reason = FailureReasonResolver.GetFailureReason(result);
                    Log.LogFallback(_logger, policyName, reason);
                    RecordMetric(PolicyEvents.FallbackPolicyEvent, policyName, result, context);
                    return options.OnFallbackAsync(new FallbackTaskArguments<TResult>(result, context, CancellationToken.None));
                });
    }

    public IAsyncPolicy CreateRetryPolicy(string policyName, RetryPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        PolicyFactoryUtility.ValidateOptions(_retryOptionsValidator, options);

        return CreateRetryPolicyWithDefaultDelay(policyName, options);
    }

    public IAsyncPolicy<TResult> CreateRetryPolicy<TResult>(string policyName, RetryPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        ValidateOptions(_retryOptionsValidator, options);
        ValidateOptions(_retryOptionsCustomValidator, options);

        if (options.RetryDelayGenerator != null)
        {
            return CreateRetryPolicyWithCustomDelay(policyName, options);
        }

        return CreateRetryPolicyWithDefaultDelay(policyName, options);
    }

    public IAsyncPolicy CreateHedgingPolicy(
        string policyName,
        HedgedTaskProvider provider,
        HedgingPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(options);

        PolicyFactoryUtility.ValidateOptions(_hedgingOptionsValidator, options);

        var hedgingDelay = options.HedgingDelay;

        return
            GetPolicyBuilderWithErrorHandling(options.ShouldHandleException)
           .AsyncHedgingPolicy(
                provider,
                options.MaxHedgedAttempts,

                // Stryker disable once all: https://domoreexp.visualstudio.com/R9/_workitems/edit/2804465
                options.HedgingDelayGenerator ?? (_ => hedgingDelay),
                (exception, context, attempt, cancellationToken) =>
                {
                    var reason = FailureReasonResolver.GetFailureFromException(exception);
                    Log.LogHedging(_logger, policyName, reason);
                    RecordMetric(PolicyEvents.HedgingPolicyEvent, policyName, exception, context);
                    return options.OnHedgingAsync(new HedgingTaskArguments(exception, context, attempt, cancellationToken));
                });
    }

    public IAsyncPolicy<TResult> CreateHedgingPolicy<TResult>(
        string policyName,
        HedgedTaskProvider<TResult> provider,
        HedgingPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(provider);
        _ = Throw.IfNull(options);

        ValidateOptions(_hedgingOptionsValidator, options);

        var hedgingDelay = options.HedgingDelay;

        return
            GetPolicyBuilderWithErrorHandling(
                options.ShouldHandleResultAsError,
                options.ShouldHandleException)
           .AsyncHedgingPolicy(
                provider,
                options.MaxHedgedAttempts,

                // Stryker disable once all: https://domoreexp.visualstudio.com/R9/_workitems/edit/2804465
                options.HedgingDelayGenerator ?? (_ => hedgingDelay),
                (result, context, attempt, cancellationToken) =>
                {
                    var reason = FailureReasonResolver.GetFailureReason(result);
                    Log.LogHedging(_logger, policyName, reason);
                    RecordMetric(PolicyEvents.HedgingPolicyEvent, policyName, result, context);
                    return options.OnHedgingAsync(new HedgingTaskArguments<TResult>(result, context, attempt, cancellationToken));
                });
    }

    public IAsyncPolicy CreateTimeoutPolicy(string policyName, TimeoutPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        PolicyFactoryUtility.ValidateOptions(_timeoutOptionsValidator, options);

        return Policy.TimeoutAsync(options.TimeoutInterval, options.TimeoutStrategy, (context, _, _) =>
        {
            return PolicyFactoryUtility.OnTimeoutAsync(context, policyName, options, _logger, _recordMetric);
        });
    }

    public IAsyncPolicy CreateBulkheadPolicy(string policyName, BulkheadPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        PolicyFactoryUtility.ValidateOptions(_bulkheadOptionsValidator, options);

        return Policy.BulkheadAsync(
            options.MaxConcurrency,
            options.MaxQueuedActions,
            context =>
            {
                return PolicyFactoryUtility.OnBulkheadRejectedAsync(context, policyName, options, _logger, _recordMetric);
            });
    }

    private static void DisposeResult<TResult>(DelegateResult<TResult> delegateResult)
    {
        if (delegateResult.Result is IDisposable disposableResult)
        {
            disposableResult.Dispose();
        }
    }

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ValidationException">When <paramref name="options"/> are not valid.</exception>
    private static void ValidateOptions<TOptions>(IValidateOptions<TOptions> validator, TOptions options)
        where TOptions : class, new()
    {
        validator.Validate(null, options).ThrowIfFailed();
    }

    private static PolicyBuilder GetPolicyBuilderWithErrorHandling(Predicate<Exception> shouldHandleExp)
    {
        return Policy.Handle<Exception>(ex => shouldHandleExp(ex));
    }

    /// <summary>
    /// Creates the policy builder with error handling.
    /// </summary>
    /// <param name="shouldHandleResult">Predicate to define what type of result shall be treated and handled as an error.</param>
    /// <param name="shouldHandleExp">Defines what exceptions should be handled.</param>
    /// <returns>
    /// A policy builder on which policies can be chained.
    /// </returns>
    private static PolicyBuilder<TResult> GetPolicyBuilderWithErrorHandling<TResult>(Predicate<TResult> shouldHandleResult, Predicate<Exception> shouldHandleExp)
    {
        return Policy.HandleResult<TResult>(r => shouldHandleResult(r)).Or<Exception>(ex => shouldHandleExp(ex));
    }

    private AsyncRetryPolicy<TResult> CreateRetryPolicyWithCustomDelay<TResult>(string policyName, RetryPolicyOptions<TResult> options)
    {
        var policyBase = GetPolicyBuilderWithErrorHandling(options.ShouldHandleResultAsError, options.ShouldHandleException);

        TimeSpan sleepDurationProvider(int attemptCount, DelegateResult<TResult> response, Context context)
        {
            var customDelay = options.RetryDelayGenerator!(
                new RetryDelayArguments<TResult>(response, context, CancellationToken.None));

            // If the generator returns an invalid delay, use the default one based on the backoff
            return customDelay > TimeSpan.Zero ? customDelay : options.BaseDelay;
        }

        Task onRetryAsync(DelegateResult<TResult> result, TimeSpan timeSpan, int attemptNumber, Context context)
            => HandleRetryEventAsync(policyName, options, result, context, timeSpan, attemptNumber);

        Task onInfiniteRetryAsync(DelegateResult<TResult> result, int attemptNumber, TimeSpan timeSpan, Context context) => onRetryAsync(result, timeSpan, attemptNumber, context);

#pragma warning disable R9A034 // Optimize method group use to avoid allocations
        if (options.RetryCount == RetryPolicyOptions.InfiniteRetry)
        {
            return policyBase.WaitAndRetryForeverAsync(sleepDurationProvider, onInfiniteRetryAsync);
        }
        else
        {
            return policyBase.WaitAndRetryAsync(options.RetryCount, sleepDurationProvider, onRetryAsync);
        }
#pragma warning restore R9A034 // Optimize method group use to avoid allocations
    }

    private AsyncRetryPolicy CreateRetryPolicyWithDefaultDelay(string policyName, RetryPolicyOptions options)
    {
        var delay = options.GetDelays();

        return GetPolicyBuilderWithErrorHandling(options.ShouldHandleException)
            .WaitAndRetryAsync(delay, (exception, timeSpan, attemptNumber, context) =>
                HandleRetryEventAsync(policyName, options, exception, context, timeSpan, attemptNumber));
    }

    private AsyncRetryPolicy<TResult> CreateRetryPolicyWithDefaultDelay<TResult>(string policyName, RetryPolicyOptions<TResult> options)
    {
        var policyBase = GetPolicyBuilderWithErrorHandling(options.ShouldHandleResultAsError, options.ShouldHandleException);

        if (options.RetryCount == RetryPolicyOptions.InfiniteRetry)
        {
            return policyBase
                .WaitAndRetryForeverAsync((_, _, _) => options.BaseDelay,
                (result, attemptNumber, timeSpan, context) =>
                    HandleRetryEventAsync(policyName, options, result, context, timeSpan, attemptNumber));
        }

        var delays = options.GetDelays();

        return policyBase
            .WaitAndRetryAsync(delays, (result, timeSpan, attemptNumber, context) =>
                HandleRetryEventAsync(policyName, options, result, context, timeSpan, attemptNumber));
    }

    private Task HandleRetryEventAsync(
        string policyName,
        RetryPolicyOptions options,
        Exception exception,
        Context context,
        TimeSpan timeSpan,
        int attemptNumber)
    {
        var reason = FailureReasonResolver.GetFailureFromException(exception);
        Log.LogRetry(_logger, policyName, reason, timeSpan.TotalSeconds, attemptNumber);
        RecordMetric(PolicyEvents.RetryPolicyEvent, policyName, exception, context);

        var arguments = new RetryActionArguments(exception, context, timeSpan, attemptNumber, CancellationToken.None);
        return options.OnRetryAsync(arguments);
    }

    private async Task HandleRetryEventAsync<TResult>(
        string policyName,
        RetryPolicyOptions<TResult> options,
        DelegateResult<TResult> result,
        Context context,
        TimeSpan timeSpan,
        int attemptNumber)
    {
        var reason = FailureReasonResolver.GetFailureReason(result);
        Log.LogRetry(_logger, policyName, reason, timeSpan.TotalSeconds, attemptNumber);
        RecordMetric(PolicyEvents.RetryPolicyEvent, policyName, result, context);

        var arguments = new RetryActionArguments<TResult>(result, context, timeSpan, attemptNumber, CancellationToken.None);

        await options.OnRetryAsync(arguments).ConfigureAwait(false);

        DisposeResult(result);
    }

    private void RecordMetric(string eventType, string policyName, Exception fault, Context context)
    {
        _metering.RecordEvent(policyName, eventType, fault, context);
    }

    private void RecordMetric(string eventType, string policyName, Context context)
    {
        _metering.RecordEvent(policyName, eventType, null, context);
    }

    private void RecordMetric<TResult>(string eventType, string policyName, DelegateResult<TResult> result, Context context)
    {
        _metering.RecordEvent(policyName, eventType, result, context);
    }
}
