// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IRealtimeClient"/> for the OpenAI Realtime API.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OpenAIRealtimeClient : IRealtimeClient
{
    /// <summary>The API key used for authentication.</summary>
    private readonly string _apiKey;

    /// <summary>The model to use for realtime sessions.</summary>
    private readonly string _model;

    /// <summary>Initializes a new instance of the <see cref="OpenAIRealtimeClient"/> class.</summary>
    /// <param name="apiKey">The API key used for authentication.</param>
    /// <param name="model">The model to use for realtime sessions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public OpenAIRealtimeClient(string apiKey, string model)
    {
        _apiKey = Throw.IfNull(apiKey);
        _model = Throw.IfNull(model);
    }

    /// <inheritdoc />
    public async Task<IRealtimeSession?> CreateSessionAsync(RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var session = new OpenAIRealtimeSession(_apiKey, _model);
        try
        {
            bool connected = await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                await session.DisposeAsync().ConfigureAwait(false);
                return null;
            }

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
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Client itself has no resources to dispose.
        // Sessions are disposed independently.
    }
}
