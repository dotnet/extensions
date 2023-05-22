// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class PipelineMetering : IPipelineMetering
{
    private static readonly RequestMetadata _fallbackMetadata = new();

    private readonly IExceptionSummarizer _exceptionSummarizer;
    private readonly IOutgoingRequestContext? _outgoingRequestContext;
    private readonly PipelinesHistogram _histogram;
    private PipelineId? _pipelineId;

    public PipelineMetering(Meter<PipelineMetering> meter, IExceptionSummarizer exceptionSummarizer, IEnumerable<IOutgoingRequestContext> outgoingContexts)
    {
        _histogram = Metric.CreatePipelinesHistogram(meter);
        _exceptionSummarizer = exceptionSummarizer;
        _outgoingRequestContext = outgoingContexts.FirstOrDefault();
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

    public void RecordPipelineExecution(long executionTimeInMs, Exception? fault, Context context)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("This instance is not initialized.");
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

        _histogram.Record(
            executionTimeInMs,
            _pipelineId!.PipelineName,
            _pipelineId!.PipelineKey.GetDimensionOrUnknown(),
            _pipelineId!.ResultType.GetDimensionOrUnknown(),
            failureSource.GetDimensionOrUnknown(),
            failureReason.GetDimensionOrUnknown(),
            failureSummary.GetDimensionOrUnknown(),
            requestMetadata.DependencyName,
            requestMetadata.RequestName);
    }

    private RequestMetadata GetRequestMetadata(Context context)
    {
        if (context.TryGetValue(TelemetryConstants.RequestMetadataKey, out var val) && val is RequestMetadata requestMetadata)
        {
            return requestMetadata;
        }

        return _outgoingRequestContext?.RequestMetadata ?? _fallbackMetadata;
    }
}
