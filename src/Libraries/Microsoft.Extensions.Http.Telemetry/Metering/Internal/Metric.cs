// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Internal;

internal static partial class Metric
{
    internal const string OutgoingRequestMetricName = @"R9\Http\OutgoingRequest";

    /// <summary>
    /// The host of the outgoing request.
    /// </summary>
    internal const string ReqHost = "req_host";

    /// <summary>
    /// The name of the target dependency service for the outgoing request.
    /// </summary>
    internal const string DependencyName = "dep_name";

    /// <summary>
    /// The name of the outgoing request.
    /// </summary>
    internal const string ReqName = "req_name";

    /// <summary>
    /// The response status code for the outgoing request.
    /// </summary>
    /// <remarks>
    /// This is the status code returned by the target dependency service. In case of exceptions, when
    /// no status code is available, this will be set to InternalServerError i.e. 500.
    /// </remarks>
    internal const string RspResultCode = "rsp_resultCode";

    /// <summary>
    /// Creates a new histogram instrument for an outgoing HTTP request.
    /// </summary>
    /// <param name="meter">Meter object.</param>
    /// <returns></returns>
    [Histogram(ReqHost, DependencyName, ReqName, RspResultCode, Name = OutgoingRequestMetricName)]
    public static partial OutgoingMetric CreateHistogram(Meter meter);
}
