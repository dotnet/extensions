// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Diagnostics;
using Polly;
using Polly.Telemetry;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class ResilienceMetricsEnricher : MeteringEnricher
{
    private readonly IOutgoingRequestContext? _outgoingRequestContext;
    private readonly IExceptionSummarizer? _exceptionSummarizer;

    public ResilienceMetricsEnricher(
        IOutgoingRequestContext? outgoingRequestContext = null,
        IExceptionSummarizer? exceptionSummarizer = null)
    {
        _outgoingRequestContext = outgoingRequestContext;
        _exceptionSummarizer = exceptionSummarizer;
    }

    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        var outcome = context.TelemetryEvent.Outcome;

        if (_exceptionSummarizer is not null && outcome?.Exception is Exception e)
        {
            context.Tags.Add(new(ResilienceTagNames.ErrorType, _exceptionSummarizer.Summarize(e).Description));
        }

        if ((context.TelemetryEvent.Context.GetRequestMetadata() ?? _outgoingRequestContext?.RequestMetadata) is RequestMetadata requestMetadata)
        {
            context.Tags.Add(new(ResilienceTagNames.RequestName, requestMetadata.RequestName));
            context.Tags.Add(new(ResilienceTagNames.DependencyName, requestMetadata.DependencyName));
        }
    }
}
