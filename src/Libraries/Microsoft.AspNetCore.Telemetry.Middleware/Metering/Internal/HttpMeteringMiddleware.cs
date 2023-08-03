// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;
using Microsoft.Shared.Text;

namespace Microsoft.AspNetCore.Telemetry.Internal;

/// <summary>
/// Records the duration of all incoming requests and logs as a metric.
/// </summary>
internal sealed class HttpMeteringMiddleware : IMiddleware
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private const int StandardDimensionsCount = 4;
    private const int MaxCustomDimensionsCount = 15;

    private readonly Histogram<long>? _incomingRequestMetric;
    private readonly IIncomingRequestMetricEnricher[]? _requestMetricEnrichers;
    private readonly ObjectPool<MetricEnrichmentTagCollector> _propertyBagPool = PoolFactory.CreateResettingPool<MetricEnrichmentTagCollector>();

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMeteringMiddleware"/> class.
    /// A middleware that records incoming request duration.
    /// </summary>
    /// <param name="meter">Meter used for metric logging.</param>
    /// <param name="requestMetricEnrichers">Enumerable of request metric enrichers.</param>
    public HttpMeteringMiddleware(Meter<HttpMeteringMiddleware> meter, IEnumerable<IIncomingRequestMetricEnricher> requestMetricEnrichers)
    {
        int dimensionsCount = StandardDimensionsCount;
        int enrichersCount = 0;
        foreach (var enricher in requestMetricEnrichers)
        {
            enrichersCount++;
            dimensionsCount += enricher.TagNames.Count;
        }

        if (dimensionsCount > MaxCustomDimensionsCount + StandardDimensionsCount)
        {
            Throw.ArgumentOutOfRangeException(
                $"Total dimensions added by all request metric enrichers should be smaller than {MaxCustomDimensionsCount}. Observed count: {dimensionsCount - StandardDimensionsCount}",
                nameof(requestMetricEnrichers));
        }

        var dimensionsSet = new HashSet<string>
        {
            Metric.ReqHost,
            Metric.ReqName,
            Metric.RspResultCode,
            Metric.ExceptionType
        };

        if (enrichersCount > 0)
        {
            _requestMetricEnrichers = new IIncomingRequestMetricEnricher[enrichersCount];

            int enricherIndex = 0;
            foreach (var enricher in requestMetricEnrichers)
            {
                _requestMetricEnrichers[enricherIndex++] = enricher;
                foreach (var dimensionName in enricher.TagNames)
                {
                    if (!dimensionsSet.Add(dimensionName))
                    {
                        Throw.ArgumentException(dimensionName, $"A dimension with name {dimensionName} already exists in one of the registered request metric enricher");
                    }
                }
            }
        }

        _incomingRequestMetric = meter.CreateHistogram<long>(Metric.IncomingRequestMetricName);
    }

    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var startTimestamp = TimeProvider.GetTimestamp();

        try
        {
            await next(context).ConfigureAwait(false);
            OnRequestEnd(context, startTimestamp, context.Response.StatusCode, null);
        }
        catch (Exception ex)
        {
            int resultCode = context.Response.StatusCode < StatusCodes.Status400BadRequest ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            OnRequestEnd(context, startTimestamp, resultCode, ex.GetType());

            throw;
        }
    }

    private void OnRequestEnd(HttpContext httpContext, long timestamp, int resultCode, Type? exceptionType)
    {
        string requestHost = string.IsNullOrWhiteSpace(httpContext.Request.Host.Value) ? "unknown_host_name" : httpContext.Request.Host.Value;
        string requestName = $"{httpContext.Request.Method} {httpContext.GetRouteTemplate() ?? "unsupported_route"}";
        string responseResultCode = resultCode.ToInvariantString();
        string exceptionTypeName = exceptionType?.FullName ?? "no_exception";
        long duration = (long)TimeProvider.GetElapsedTime(timestamp, TimeProvider.GetTimestamp()).TotalMilliseconds;

        var tagList = new TagList
        {
            new(Metric.ReqHost, requestHost),
            new(Metric.ReqName, requestName),
            new(Metric.RspResultCode, responseResultCode),
            new(Metric.ExceptionType, exceptionTypeName),
        };

        // keep default case fast by avoiding allocations
        if (_requestMetricEnrichers == null)
        {
            _incomingRequestMetric!.Record(value: duration, tagList);
        }
        else
        {
            var requestEnrichmentPropertyBag = _propertyBagPool.Get();
            try
            {
                foreach (var enricher in _requestMetricEnrichers)
                {
                    enricher.Enrich(requestEnrichmentPropertyBag);
                }

                foreach (var item in requestEnrichmentPropertyBag)
                {
                    tagList.Add(item.Key, item.Value);
                }

                _incomingRequestMetric!.Record(value: duration, tagList);
            }
            finally
            {
                _propertyBagPool.Return(requestEnrichmentPropertyBag);
            }
        }
    }
}
