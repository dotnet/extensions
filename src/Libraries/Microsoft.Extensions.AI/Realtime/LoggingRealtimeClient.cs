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

/// <summary>A delegating realtime client that logs operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="LogLevel.Trace"/>, the contents of
/// messages and options are logged. These messages and options may contain sensitive application data.
/// <see cref="LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class LoggingRealtimeClient : DelegatingRealtimeClient
{
    private readonly ILogger _logger;
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingRealtimeClient"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IRealtimeClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingRealtimeClient(IRealtimeClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
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
        return new LoggingRealtimeClientSession(innerSession, _logger)
        {
            JsonSerializerOptions = _jsonSerializerOptions,
        };
    }
}
