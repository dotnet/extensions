// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Telemetry.Metering.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Handler that logs outgoing request duration.
/// </summary>
/// <seealso cref="DelegatingHandler" />
public class HttpMeteringHandler : DelegatingHandler
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private const int StandardDimensionsCount = 4;
    private const int MaxCustomDimensionsCount = 14;

    private static readonly RequestMetadata _fallbackMetadata = new();

    private readonly Histogram<long> _outgoingRequestMetric;
    private readonly IOutgoingRequestMetricEnricher[] _requestEnrichers;
    private readonly ObjectPool<MetricEnrichmentPropertyBag> _propertyBagPool = PoolFactory.CreateResettingPool<MetricEnrichmentPropertyBag>();
    private readonly IOutgoingRequestContext? _requestMetadataContext;
    private readonly IDownstreamDependencyMetadataManager? _downstreamDependencyMetadataManager;
    private readonly int _enrichersCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMeteringHandler"/> class.
    /// </summary>
    /// <param name="meter">The meter.</param>
    /// <param name="enrichers">Enumerable of outgoing request metric enrichers.</param>
    [Experimental]
    public HttpMeteringHandler(
        Meter<HttpMeteringHandler> meter,
        IEnumerable<IOutgoingRequestMetricEnricher> enrichers)
        : this(meter, enrichers, null, null)
    {
    }

    internal HttpMeteringHandler(
        Meter<HttpMeteringHandler> meter,
        IEnumerable<IOutgoingRequestMetricEnricher> enrichers,
        IOutgoingRequestContext? requestMetadataContext,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        _ = Throw.IfNull(meter);
        _ = Throw.IfNull(enrichers);

        _requestEnrichers = enrichers.ToArray();
        int dimensionsCount = StandardDimensionsCount;

        foreach (var enricher in _requestEnrichers)
        {
            _enrichersCount++;
            dimensionsCount += enricher.DimensionNames.Count;
        }

        if (dimensionsCount > MaxCustomDimensionsCount + StandardDimensionsCount)
        {
            Throw.ArgumentOutOfRangeException(
                $"Total dimensions added by all outgoing request metric enrichers should be smaller than {MaxCustomDimensionsCount}. Observed count: {dimensionsCount - StandardDimensionsCount}",
                nameof(enrichers));
        }

        var dimensionsSet = new HashSet<string>
        {
            Metric.ReqHost,
            Metric.DependencyName,
            Metric.ReqName,
            Metric.RspResultCode
        };

        for (int i = 0; i < _requestEnrichers.Length; i++)
        {
            foreach (var dimensionName in _requestEnrichers[i].DimensionNames)
            {
                if (!dimensionsSet.Add(dimensionName))
                {
                    Throw.ArgumentException(nameof(enrichers), $"A dimension with name {dimensionName} already exists in one of the registered outgoing request metric enrichers");
                }
            }
        }

        _outgoingRequestMetric = meter.CreateHistogram<long>(Metric.OutgoingRequestMetricName);

        _requestMetadataContext = requestMetadataContext;
        _downstreamDependencyMetadataManager = downstreamDependencyMetadataManager;
    }

    internal static string GetHostName(HttpRequestMessage request) => string.IsNullOrWhiteSpace(request.RequestUri?.Host) ? TelemetryConstants.Unknown : request.RequestUri!.Host;

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>
    /// The task object representing the asynchronous operation.
    /// </returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(request);

        var timestamp = TimeProvider.GetTimestamp();

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            OnRequestEnd(request, timestamp, response.StatusCode);
            return response;
        }
        catch
        {
            // This will not catch a response that returns 4xx, 5xx, etc. but will only catch when base.SendAsync() fails.
            OnRequestEnd(request, timestamp, HttpStatusCode.InternalServerError);
            throw;
        }
    }

    private void OnRequestEnd(HttpRequestMessage request, long timestamp, HttpStatusCode statusCode)
    {
        var requestMetadata = request.GetRequestMetadata() ??
            _requestMetadataContext?.RequestMetadata ??
            _downstreamDependencyMetadataManager?.GetRequestMetadata(request) ??
            _fallbackMetadata;
        var dependencyName = requestMetadata.DependencyName;
        var requestName = $"{request.Method} {requestMetadata.GetRequestName()}";
        var hostName = GetHostName(request);
        var duration = (long)TimeProvider.GetElapsedTime(timestamp, TimeProvider.GetTimestamp()).TotalMilliseconds;

        var tagList = new TagList
        {
            new(Metric.ReqHost, hostName),
            new(Metric.DependencyName, dependencyName),
            new(Metric.ReqName, requestName),
            new(Metric.RspResultCode, (int)statusCode)
        };

        // keep default case fast by avoiding allocations
        if (_enrichersCount == 0)
        {
            _outgoingRequestMetric.Record(value: duration, tagList);
        }
        else
        {
            var propertyBag = _propertyBagPool.Get();
            try
            {
                foreach (var enricher in _requestEnrichers)
                {
                    enricher.Enrich(propertyBag);
                }

                foreach (var item in propertyBag)
                {
                    tagList.Add(item.Key, item.Value);
                }

                _outgoingRequestMetric.Record(value: duration, tagList);
            }
            finally
            {
                _propertyBagPool.Return(propertyBag);
            }
        }
    }
}
