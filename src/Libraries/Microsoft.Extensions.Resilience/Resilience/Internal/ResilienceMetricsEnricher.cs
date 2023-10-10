// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Polly.Telemetry;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class ResilienceMetricsEnricher : MeteringEnricher
{
    private readonly FrozenDictionary<Type, Func<object, FailureResultContext>> _faultFactories;
    private readonly IOutgoingRequestContext? _outgoingRequestContext;
    private readonly IExceptionSummarizer? _exceptionSummarizer;

    public ResilienceMetricsEnricher(
        IOptions<FailureEventMetricsOptions> metricsOptions,
        IEnumerable<IOutgoingRequestContext> outgoingRequestContext,
        IExceptionSummarizer? exceptionSummarizer = null)
    {
        _faultFactories = metricsOptions.Value.Factories.ToFrozenDictionary();
        _outgoingRequestContext = outgoingRequestContext.FirstOrDefault();
        _exceptionSummarizer = exceptionSummarizer;
    }

    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        var outcome = context.TelemetryEvent.Outcome;

        if (_exceptionSummarizer is not null && outcome?.Exception is Exception e)
        {
            context.Tags.Add(new(ResilienceTagNames.FailureSource, e.Source));
            context.Tags.Add(new(ResilienceTagNames.FailureReason, e.GetType().Name));
            context.Tags.Add(new(ResilienceTagNames.FailureSummary, _exceptionSummarizer.Summarize(e).ToString()));
        }
        else if (outcome is not null && outcome.Value.Result is object result && _faultFactories.TryGetValue(result.GetType(), out var factory))
        {
            var failureContext = factory(result);
            context.Tags.Add(new(ResilienceTagNames.FailureSource, failureContext.FailureSource));
            context.Tags.Add(new(ResilienceTagNames.FailureReason, failureContext.FailureReason));
            context.Tags.Add(new(ResilienceTagNames.FailureSummary, failureContext.AdditionalInformation));
        }

        if ((context.TelemetryEvent.Context.GetRequestMetadata() ?? _outgoingRequestContext?.RequestMetadata) is RequestMetadata requestMetadata)
        {
            context.Tags.Add(new(ResilienceTagNames.RequestName, requestMetadata.RequestName));
            context.Tags.Add(new(ResilienceTagNames.DependencyName, requestMetadata.DependencyName));
        }
    }
}
