// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Common telemetry constants used by various telemetry libraries.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// Request metadata key that is used when storing request metadata object.
    /// </summary>
    public const string RequestMetadataKey = "R9-RequestMetadata";

    /// <summary>
    /// Placeholder string for unknown request name, dependency name etc. in telemetry.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Placeholder string used for redacted data where needed.
    /// </summary>
    public const string Redacted = "REDACTED";

    /// <summary>
    /// Header for client application name, sent on an outgoing http call.
    /// </summary>
    [Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
    public const string ClientApplicationNameHeader = "X-ClientApplication";

    /// <summary>
    /// Header for server application name, sent on a http request.
    /// </summary>
    [Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
    public const string ServerApplicationNameHeader = "X-ServerApplication";
}
