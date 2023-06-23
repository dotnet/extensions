// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Interface for implementing a redaction mechanism for outgoing HTTP request paths.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
public interface IHttpPathRedactor
{
    /// <summary>
    /// Redact <paramref name="parametersToRedact"/> of <paramref name="routeTemplate"/> found in <paramref name="httpPath"/>.
    /// </summary>
    /// <param name="routeTemplate">HTTP route template such as "/api/v1/users/{userId}".</param>
    /// <param name="httpPath">HTTP request path such as "/api/v1/users/my-user-id".</param>
    /// <param name="parametersToRedact">Parameters to redact, such as "userId".</param>
    /// <param name="parameterCount">Number of parameters found in <paramref name="routeTemplate"/>.</param>
    /// <returns>Redacted HTTP request path, such as "/api/v1/users/redacted-user-id".</returns>
    string Redact(string routeTemplate, string httpPath, IReadOnlyDictionary<string, DataClassification> parametersToRedact, out int parameterCount);
}
