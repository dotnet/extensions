// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Statuses for classifying http request result.
/// </summary>
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
[EnumStrings]
public enum HttpRequestResultType
{
    /// <summary>
    /// The status code of the http request indicates that the request is successful.
    /// </summary>
    Success,

    /// <summary>
    /// The status code of the http request indicates that this request did not succeed and to be treated as failure.
    /// </summary>
    Failure,

    /// <summary>
    /// The status code of the http request indicates that the request did not succeed but has failed with an error which is expected and acceptable for this request.
    /// </summary>
    /// <remarks>
    /// Expected failures are generally excluded from availability calculations i.e. they are neither
    /// treated as success nor as failures for availability calculation.
    /// </remarks>
    ExpectedFailure,
}
