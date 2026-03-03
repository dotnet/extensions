// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI.Realtime;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable OPENAI002 // OpenAI Realtime API is experimental

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IRealtimeClient"/> for the OpenAI Realtime API.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OpenAIRealtimeClient : IRealtimeClient
{
    /// <summary>The OpenAI Realtime client.</summary>
    private readonly RealtimeClient _realtimeClient;

    /// <summary>The model to use for realtime sessions.</summary>
    private readonly string _model;

    /// <summary>Metadata about this client's provider and model, used for OpenTelemetry.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>Initializes a new instance of the <see cref="OpenAIRealtimeClient"/> class.</summary>
    /// <param name="apiKey">The API key used for authentication.</param>
    /// <param name="model">The model to use for realtime sessions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public OpenAIRealtimeClient(string apiKey, string model)
    {
        _realtimeClient = new RealtimeClient(Throw.IfNull(apiKey));
        _model = Throw.IfNull(model);
        _metadata = new("openai", defaultModelId: _model);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAIRealtimeClient"/> class.</summary>
    /// <param name="realtimeClient">The OpenAI Realtime client to use.</param>
    /// <param name="model">The model to use for realtime sessions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="realtimeClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public OpenAIRealtimeClient(RealtimeClient realtimeClient, string model)
    {
        _realtimeClient = Throw.IfNull(realtimeClient);
        _model = Throw.IfNull(model);
        _metadata = new("openai", defaultModelId: _model);
    }

    /// <inheritdoc />
    public async Task<IRealtimeSession> CreateSessionAsync(RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var sessionClient = options?.SessionKind == RealtimeSessionKind.Transcription
            ? await _realtimeClient.StartTranscriptionSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false)
            : await _realtimeClient.StartConversationSessionAsync(_model, cancellationToken: cancellationToken).ConfigureAwait(false);

        var session = new OpenAIRealtimeSession(sessionClient, _model);
        try
        {
            if (options is not null)
            {
                await session.UpdateAsync(options, cancellationToken).ConfigureAwait(false);
            }

            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    object? IRealtimeClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType.IsInstanceOfType(this) ? this :
            serviceType.IsInstanceOfType(_realtimeClient) ? _realtimeClient :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Client itself has no resources to dispose.
        // Sessions are disposed independently.
    }
}
