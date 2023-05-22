// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.AspNetCore.Telemetry.Internal;

[ExcludeFromCodeCoverage]
internal static partial class Metric
{
    internal const string IncomingRequestMetricName = @"R9\Http\IncomingRequest";

    /// <summary>
    /// The host part of the incoming request URL.
    /// </summary>
    internal const string ReqHost = "req_host";

    /// <summary>
    /// The name of the incoming request.
    /// </summary>
    internal const string ReqName = "req_name";

    /// <summary>
    /// The response status code for the request.
    /// </summary>
    internal const string RspResultCode = "rsp_resultCode";

    /// <summary>
    /// The type of the exception thrown during processing of the request.
    /// </summary>
    internal const string ExceptionType = "env_ex_type";

    /// <summary>
    /// Creates a new histogram instrument for incoming HTTP request.
    /// </summary>
    /// <param name="meter">Meter object.</param>
    /// <returns></returns>
    [Histogram(ReqHost, ReqName, RspResultCode, ExceptionType, Name = IncomingRequestMetricName)]
    public static partial IncomingRequestMetric CreateHistogram(Meter meter);
}
