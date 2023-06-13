// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Holds request metadata for use by the telemetry system.
/// </summary>
public class RequestMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestMetadata"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor initializes <see cref="MethodType"/> to <c>GET</c>, and all other properties to <c>"unknown"</c>.
    /// </remarks>
    public RequestMetadata()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestMetadata"/> class.
    /// </summary>
    /// <param name="methodType">Http method type of the request.</param>
    /// <param name="requestRoute">Route of the request.</param>
    /// <param name="requestName">Name of the request.</param>
    /// <remarks>
    /// The <see cref="DependencyName"/> property is initialized to <c>"unknown"</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public RequestMetadata(string methodType, string requestRoute, string requestName = TelemetryConstants.Unknown)
    {
        MethodType = Throw.IfNull(methodType);
        RequestRoute = Throw.IfNull(requestRoute);
        RequestName = Throw.IfNull(requestName);
    }

    /// <summary>
    /// Gets or sets request's route template.
    /// </summary>
    /// <remarks>
    /// Request Route is used for multiple use cases.
    /// - For outgoing request metrics, it is used as the request name dimension (if RequestName is not provided).
    /// - For Logs and traces, it is used to identify sensitive parameters from the path and redact them in the exported path, so sensitive data leakage can be avoided.
    /// If you are using redaction the template should be accurate for the request else redaction won't be applied to sensitive parameters.
    /// e.g. A template would look something like /v1/users/{userId}/chats/{chatId}/messages. The sensitive parameter names should match exactly as provided
    /// in configuration for outgoing tracing and outgoing logging autocollectors for parameters to be redacted.
    /// </remarks>
    public string RequestRoute { get; set; } = TelemetryConstants.Unknown;

    /// <summary>
    /// Gets or sets name to be logged for the request.
    /// </summary>
    /// <remarks>
    /// RequestName is used in the following manner by outgoing http request auto collectors:
    /// - For outgoing request metrics: RequestName is used as the request name dimension if present, if not provided RequestRoute value would be used instead.
    /// - For outgoing request traces: It is used as the Display name for the activity i.e. When looking at the E2E trace flow this name is used in the Tree view of traces.
    ///   if not provided RequestRoute value would be used instead.
    /// - For outgoing request logs: When present it would be added as an additional dimension to logs.
    /// </remarks>
    public string RequestName { get; set; } = TelemetryConstants.Unknown;

    /// <summary>
    /// Gets or sets name of the dependency to which the outgoing request is being made.
    /// </summary>
    /// <remarks>
    /// DependencyName is used in the following manner by outgoing http request auto collectors:
    /// - For outgoing request metrics: This is added as dependency name dimension so metrics can be pivoted based on the dependency.
    /// - For outgoing request traces and logs: This is added as dependency name dimension for better diagnosability.
    /// </remarks>
    public string DependencyName { get; set; } = TelemetryConstants.Unknown;

    /// <summary>
    /// Gets or sets the http method type of the request.
    /// </summary>
    /// <remarks>
    /// Supported types are GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS, TRACE.
    /// </remarks>
    public string MethodType { get; set; } = "GET";
}
