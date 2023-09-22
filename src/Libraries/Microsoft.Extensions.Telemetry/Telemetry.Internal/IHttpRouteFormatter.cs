// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Http request route formatter.
/// </summary>
internal interface IHttpRouteFormatter
{
    /// <summary>
    /// Format the http path using the route template with sensitive parameters redacted.
    /// </summary>
    /// <param name="httpRoute">Http request route template.</param>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <returns>Returns formatted path with sensitive parameter values redacted.</returns>
    string Format(string httpRoute, string httpPath, HttpRouteParameterRedactionMode redactionMode, IReadOnlyDictionary<string, DataClassification> parametersToRedact);

    /// <summary>
    /// Format the http path using the route template with sensitive parameters redacted.
    /// </summary>
    /// <param name="routeSegments">Http request's route segments.</param>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <returns>Returns formatted path with sensitive parameter values redacted.</returns>
    string Format(in ParsedRouteSegments routeSegments, string httpPath, HttpRouteParameterRedactionMode redactionMode, IReadOnlyDictionary<string, DataClassification> parametersToRedact);
}
