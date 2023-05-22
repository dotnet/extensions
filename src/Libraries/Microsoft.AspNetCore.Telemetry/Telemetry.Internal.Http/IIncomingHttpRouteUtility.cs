// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Telemetry.Internal;

/// <summary>
/// Telemetry utilities for incoming http requests.
/// </summary>
internal interface IIncomingHttpRouteUtility
{
    /// <summary>
    /// Gets a dictionary of sensitive parameters in the route with the data class.
    /// </summary>
    /// <param name="httpRoute">Http request's route template.</param>
    /// <param name="request">Http request object.</param>
    /// <param name="defaultSensitiveParameters">Default sensitive parameters to be applied to all routes.</param>
    /// <returns>A dictionary of parameter name to data class containing all sensitive parameters in the given route.</returns>
    IReadOnlyDictionary<string, DataClassification> GetSensitiveParameters(string httpRoute, HttpRequest request, IReadOnlyDictionary<string, DataClassification> defaultSensitiveParameters);
}
