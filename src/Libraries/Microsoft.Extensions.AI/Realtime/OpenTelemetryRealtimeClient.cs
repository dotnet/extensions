// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating realtime client that adds OpenTelemetry support, following the OpenTelemetry Semantic Conventions for Generative AI systems.
/// </summary>
/// <remarks>
/// <para>
/// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OpenTelemetryRealtimeClient : DelegatingRealtimeClient
{
    private readonly ILogger? _logger;
    private readonly string? _sourceName;
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryRealtimeClient"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IRealtimeClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting any logging data from the client.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    public OpenTelemetryRealtimeClient(IRealtimeClient innerClient, ILogger? logger = null, string? sourceName = null)
        : base(innerClient)
    {
        _logger = logger;
        _sourceName = sourceName;
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>
    /// Gets or sets a value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>, unless the <c>OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT</c>
    /// environment variable is set to "true" (case-insensitive).
    /// </value>
    public bool EnableSensitiveData { get; set; } = TelemetryHelpers.EnableSensitiveDataDefault;

    /// <summary>Gets or sets JSON serialization options to use when formatting realtime data into telemetry strings.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc />
    public override async Task<IRealtimeClientSession> CreateSessionAsync(
        RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var innerSession = await base.CreateSessionAsync(options, cancellationToken).ConfigureAwait(false);
        return new OpenTelemetryRealtimeClientSession(innerSession, _logger, _sourceName)
        {
            EnableSensitiveData = EnableSensitiveData,
            JsonSerializerOptions = _jsonSerializerOptions,
        };
    }
}
