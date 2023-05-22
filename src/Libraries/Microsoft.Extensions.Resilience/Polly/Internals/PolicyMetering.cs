// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class PolicyMetering : IPolicyMetering
{
    private static readonly RequestMetadata _fallbackMetadata = new();

    private readonly ConcurrentDictionary<Type, object> _options = new();
    private readonly IExceptionSummarizer _exceptionSummarizer;
    private readonly IOutgoingRequestContext? _outgoingRequestContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly PoliciesMetricCounter _counter;
    private PipelineId? _pipelineId;

    public PolicyMetering(
        Meter<PolicyMetering> meter,
        IExceptionSummarizer exceptionSummarizer,
        IServiceProvider serviceProvider)
    {
        _counter = Resilience.PollyMetric.CreatePoliciesMetricCounter(meter);
        _exceptionSummarizer = exceptionSummarizer;
        _outgoingRequestContext = serviceProvider.GetService<IOutgoingRequestContext>();
        _serviceProvider = serviceProvider;
    }

    private bool IsInitialized => _pipelineId != null;

    public void Initialize(PipelineId pipelineId)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("This instance is already initialized.");
        }

        _pipelineId = pipelineId;
    }

    public void RecordEvent(
        string policyName,
        string eventName,
        Exception? fault,
        Context? context)
    {
        if (!IsInitialized)
        {
            return;
        }

        string? failureSource = null;
        string? failureReason = null;
        string? failureSummary = null;

        if (fault != null)
        {
            failureSource = fault.Source;
            failureReason = fault.GetType().Name;
            failureSummary = _exceptionSummarizer.Summarize(fault).ToString();
        }

        var requestMetadata = GetRequestMetadata(context);

        _counter.Add(
            1,
            _pipelineId!.PipelineName,
            _pipelineId!.PipelineKey.GetDimensionOrUnknown(),
            _pipelineId!.ResultType.GetDimensionOrUnknown(),
            policyName,
            eventName,
            failureSource.GetDimensionOrUnknown(),
            failureReason.GetDimensionOrUnknown(),
            failureSummary.GetDimensionOrUnknown(),
            requestMetadata.DependencyName,
            requestMetadata.RequestName);
    }

    public void RecordEvent<TResult>(
        string policyName,
        string eventName,
        DelegateResult<TResult>? fault,
        Context? context)
    {
        if (!IsInitialized)
        {
            return;
        }

        string? failureSource = null;
        string? failureReason = null;
        string? failureSummary = null;

        if (fault != null)
        {
            if (fault.Exception != null)
            {
                RecordEvent(policyName, eventName, fault.Exception, context);
                return;
            }
            else
            {
                if (!Equals(fault.Result, default(TResult)))
                {
                    var failureContext = GetFailureContext(fault.Result);

                    failureSource = failureContext.FailureSource;
                    failureReason = failureContext.FailureReason;
                    failureSummary = failureContext.AdditionalInformation;
                }
            }
        }

        var requestMetadata = GetRequestMetadata(context);

        _counter.Add(
            1,
            _pipelineId!.PipelineName,
            _pipelineId!.PipelineKey.GetDimensionOrUnknown(),
            _pipelineId!.ResultType.GetDimensionOrUnknown(),
            policyName,
            eventName,
            failureSource.GetDimensionOrUnknown(),
            failureReason.GetDimensionOrUnknown(),
            failureSummary.GetDimensionOrUnknown(),
            requestMetadata.DependencyName,
            requestMetadata.RequestName);
    }

    private FailureResultContext GetFailureContext<TResult>(TResult result)
    {
        var options = (FailureEventMetricsOptions<TResult>)_options.GetOrAdd(
            typeof(TResult),
            static (_, provider) => provider.GetRequiredService<IOptions<FailureEventMetricsOptions<TResult>>>().Value,
            _serviceProvider);

        return options.GetContextFromResult(result);
    }

    private RequestMetadata GetRequestMetadata(Context? context)
    {
        if (context != null && context.TryGetValue(TelemetryConstants.RequestMetadataKey, out var val) && val is RequestMetadata requestMetadata)
        {
            return requestMetadata;
        }

        return _outgoingRequestContext?.RequestMetadata ?? _fallbackMetadata;
    }
}
