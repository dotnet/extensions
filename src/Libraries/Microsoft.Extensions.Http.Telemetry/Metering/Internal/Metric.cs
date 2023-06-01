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
    /// no status code is available, this will be set to 500.
    /// </remarks>
    internal const string RspResultCode = "rsp_resultCode";

    /// <summary>
    /// The response status category for the outgoing request.
    /// </summary>
    /// <remarks>
    /// This is the response cantegory returned by the target dependency service. It will return one of 3
    /// statuses: <c>success</c>, <c>failure</c> or <c>expectedfailure</c>.
    /// </remarks>
    internal const string RspResultCategory = "rsp_resultCategory";

    /// <summary>
    /// Creates a new histogram instrument for an outgoing HTTP request.
    /// </summary>
    /// <param name="meter">Meter object.</param>
    /// <returns></returns>
    [Histogram(ReqHost, DependencyName, ReqName, RspResultCode, RspResultCategory, Name = OutgoingRequestMetricName)]
    public static partial OutgoingMetric CreateHistogram(Meter meter);
}
