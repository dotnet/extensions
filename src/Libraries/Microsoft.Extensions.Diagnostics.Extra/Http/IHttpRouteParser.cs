// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Http request route parser.
/// </summary>
internal interface IHttpRouteParser
{
    /// <summary>
    /// Parses http route and breaks it into text and parameter segments.
    /// </summary>
    /// <param name="httpRoute">Http request's route template.</param>
    /// <returns>Returns text and parameter segments of route.</returns>
    ParsedRouteSegments ParseRoute(string httpRoute);

    /// <summary>
    /// Extract parameters values from the http request path.
    /// </summary>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="routeSegments">Route segments containing text and parameter segments of the route.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <param name="httpRouteParameters">Output array where parameters will be stored. Caller must provide the array with enough capacity to hold all parameters in route segment.</param>
    /// <returns>Returns true if parameters were extracted successfully, return false otherwise.</returns>
#pragma warning disable CA1045 // Do not pass types by reference
    bool TryExtractParameters(
        string httpPath,
        in ParsedRouteSegments routeSegments,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref HttpRouteParameter[] httpRouteParameters);
#pragma warning restore CA1045 // Do not pass types by reference
}
