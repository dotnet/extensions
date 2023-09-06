// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Metrics;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Default implementation of <see cref="IChaosPolicyFactory"/>.
/// </summary>
internal sealed class ChaosPolicyFactory : IChaosPolicyFactory
{
    private const string FaultTypeLatency = "Latency";
    private const string FaultTypeException = "Exception";
    private const string FaultTypeCustomResult = "CustomResult";

    private readonly Task<bool> _enabled = Task.FromResult(true);
    private readonly Task<bool> _notEnabled = Task.FromResult(false);
    private readonly Task<double> _noInjectionRate = Task.FromResult<double>(0);

    private readonly ILogger<IChaosPolicyFactory> _logger;
    private readonly FaultInjectionMetricCounter _counter;
    private readonly IFaultInjectionOptionsProvider _optionsProvider;
    private readonly IExceptionRegistry _exceptionRegistry;
    private readonly ICustomResultRegistry _customResultRegistry;

    private readonly Func<Context, CancellationToken, Task<TimeSpan>> _getLatencyAsync;
    private readonly Func<Context, CancellationToken, Task<double>> _getInjectionRateAsync;
    private readonly Func<Context, CancellationToken, Task<bool>> _getEnabledAsync;
    private readonly Func<Context, CancellationToken, Task<Exception>> _getExceptionAsync;
    private readonly Func<Context, CancellationToken, Task<double>> _getInjectionRateAsyncEx;
    private readonly Func<Context, CancellationToken, Task<bool>> _getEnabledAsyncEx;
    private readonly Func<Context, CancellationToken, Task<bool>> _getEnabledAsyncCustom;
    private readonly Func<Context, CancellationToken, Task<double>> _getInjectionRateAsyncCustom;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaosPolicyFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="meter">The meter.</param>
    /// <param name="optionsProvider">The provider of <see cref="FaultInjectionOptions"/>.</param>
    /// <param name="exceptionRegistry">
    /// The registry that contains registered exception instances for fault-injection.
    /// </param>
    /// <param name="customResultRegistry">
    /// The registry that contains registered custom result object instances for fault-injection.
    /// </param>
    [ExcludeFromCodeCoverage]
    public ChaosPolicyFactory(ILogger<IChaosPolicyFactory> logger, Meter<IChaosPolicyFactory> meter,
        IFaultInjectionOptionsProvider optionsProvider, IExceptionRegistry exceptionRegistry, ICustomResultRegistry customResultRegistry)
    {
        _logger = logger;
        _counter = Metric.CreateFaultInjectionMetricCounter(meter);
        _optionsProvider = optionsProvider;
        _exceptionRegistry = exceptionRegistry;
        _customResultRegistry = customResultRegistry;

        _getLatencyAsync = GetLatencyAsync;
        _getInjectionRateAsync = GetInjectionRateAsync<LatencyPolicyOptions>;
        _getEnabledAsync = GetEnabledAsync<LatencyPolicyOptions>;
        _getExceptionAsync = GetExceptionAsync;
        _getInjectionRateAsyncEx = GetInjectionRateAsync<ExceptionPolicyOptions>;
        _getEnabledAsyncEx = GetEnabledAsync<ExceptionPolicyOptions>;
        _getEnabledAsyncCustom = GetEnabledAsync<CustomResultPolicyOptions>;
        _getInjectionRateAsyncCustom = GetInjectionRateAsync<CustomResultPolicyOptions>;
    }

    /// <inheritdoc/>
    public AsyncInjectLatencyPolicy<TResult> CreateLatencyPolicy<TResult>() =>
        MonkeyPolicy.InjectLatencyAsync<TResult>(with =>
            with.Latency(_getLatencyAsync)
                .InjectionRate(_getInjectionRateAsync)
                .EnabledWhen(_getEnabledAsync));

    /// <inheritdoc/>
    public AsyncInjectOutcomePolicy CreateExceptionPolicy() =>
        MonkeyPolicy.InjectExceptionAsync(with =>
            with.Fault(_getExceptionAsync)
            .InjectionRate(_getInjectionRateAsyncEx)
            .EnabledWhen(_getEnabledAsyncEx));

    /// <inheritdoc/>
    public AsyncInjectOutcomePolicy<TResult> CreateCustomResultPolicy<TResult>() =>
        MonkeyPolicy.InjectResultAsync<TResult>(with =>
            with.Result(GetCustomResultAsync<TResult>)
            .InjectionRate(_getInjectionRateAsyncCustom)
            .EnabledWhen(_getEnabledAsyncCustom));

    /// <summary>
    /// Task for checking if fault-injection is enabled from the <see cref="Context"/>'s associated chaos policy options.
    /// </summary>
    internal Task<bool> GetEnabledAsync<TOptions>(Context context, CancellationToken _0)
    {
        var groupName = context.GetFaultInjectionGroupName();
        if (groupName == null)
        {
            return _notEnabled;
        }

        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);
        if (optionsGroup == null)
        {
            return _notEnabled;
        }

        ChaosPolicyOptionsBase? options = null;
        if (typeof(TOptions) == typeof(LatencyPolicyOptions))
        {
            options = optionsGroup.LatencyPolicyOptions;
        }
        else if (typeof(TOptions) == typeof(ExceptionPolicyOptions))
        {
            options = optionsGroup.ExceptionPolicyOptions;
        }
        else if (typeof(TOptions) == typeof(CustomResultPolicyOptions))
        {
            options = optionsGroup.CustomResultPolicyOptions;
        }

        if (options == null || !options.Enabled)
        {
            return _notEnabled;
        }

        return _enabled;
    }

    /// <summary>
    /// Task for checking the injection rate from the <see cref="Context"/>'s associated chaos policy options.
    /// </summary>
    internal Task<double> GetInjectionRateAsync<TOptions>(Context context, CancellationToken _0)
    {
        var groupName = context.GetFaultInjectionGroupName();
        if (groupName == null)
        {
            return _noInjectionRate;
        }

        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);
        if (optionsGroup == null)
        {
            return _noInjectionRate;
        }

        ChaosPolicyOptionsBase? options = null;
        if (typeof(TOptions) == typeof(LatencyPolicyOptions))
        {
            options = optionsGroup.LatencyPolicyOptions;
        }
        else if (typeof(TOptions) == typeof(ExceptionPolicyOptions))
        {
            options = optionsGroup.ExceptionPolicyOptions;
        }
        else if (typeof(TOptions) == typeof(CustomResultPolicyOptions))
        {
            options = optionsGroup.CustomResultPolicyOptions;
        }

        if (options == null)
        {
            return _noInjectionRate;
        }

        return Task.FromResult(options.FaultInjectionRate);
    }

    /// <summary>
    /// Fault provider task for <see cref="CreateLatencyPolicy()"/>.
    /// </summary>
    /// <remarks>
    /// This task only gets executed when LatencyPolicyOptions is defined at the options group and is enabled,
    /// as defined in <see cref="GetEnabledAsync"/>.
    /// See how faults are injected at <see href="https://github.com/Polly-Contrib/Simmy/blob/master/src/Polly.Contrib.Simmy/AsyncMonkeyEngine.cs"/>.
    /// </remarks>
    internal Task<TimeSpan> GetLatencyAsync(Context context, CancellationToken _0)
    {
        var groupName = context.GetFaultInjectionGroupName()!;
        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);

        var latency = optionsGroup!.LatencyPolicyOptions!.Latency;

        FaultInjectionTelemetryHandler.LogAndMeter(
            _logger, _counter, groupName,
            FaultTypeLatency, latency.ToString());

        return Task.FromResult(latency);
    }

    /// <summary>
    /// Fault provider task for <see cref="CreateExceptionPolicy()"/>.
    /// </summary>
    /// <remarks>
    /// This task only gets executed when ExceptionPolicyOptions is defined at the options group and is enabled,
    /// as defined in <see cref="GetEnabledAsync"/>.
    /// If exception is null, the result will simply be ignored by Simmy's AsyncMonkeyEngine.
    /// See how faults are injected at <see href="https://github.com/Polly-Contrib/Simmy/blob/master/src/Polly.Contrib.Simmy/AsyncMonkeyEngine.cs"/>.
    /// </remarks>
    internal Task<Exception> GetExceptionAsync(Context context, CancellationToken _0)
    {
        var groupName = context.GetFaultInjectionGroupName()!;
        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);

        // Exception is not going to be null
        var exception = _exceptionRegistry.GetException(optionsGroup!.ExceptionPolicyOptions!.ExceptionKey);

        FaultInjectionTelemetryHandler.LogAndMeter(
            _logger, _counter, groupName,
            FaultTypeException, exception.GetType().FullName!);

        return Task.FromResult(exception);
    }

    /// <summary>
    /// Fault provider task for <see cref="CreateCustomResultPolicy{TResult}"/>.
    /// </summary>
    /// <remarks>
    /// This task only gets executed when CustomResultPolicyOptions is defined at the options group and is enabled,
    /// as defined in <see cref="GetEnabledAsync"/>.
    /// If the result is null, the result will simply be ignored by Simmy's AsyncMonkeyEngine.
    /// See how faults are injected at <see href="https://github.com/Polly-Contrib/Simmy/blob/master/src/Polly.Contrib.Simmy/AsyncMonkeyEngine.cs"/>.
    /// </remarks>
    internal Task<TResult> GetCustomResultAsync<TResult>(Context context, CancellationToken _0)
    {
        var groupName = context.GetFaultInjectionGroupName()!;
        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);

        var customResultObj = _customResultRegistry.GetCustomResult(optionsGroup!.CustomResultPolicyOptions!.CustomResultKey);

        FaultInjectionTelemetryHandler.LogAndMeter(
            _logger, _counter, groupName,
            FaultTypeCustomResult, optionsGroup!.CustomResultPolicyOptions!.CustomResultKey);

        if (customResultObj is TResult customResult)
        {
            return Task.FromResult(customResult);
        }

        return Task.FromResult(default(TResult)!);
    }
}
