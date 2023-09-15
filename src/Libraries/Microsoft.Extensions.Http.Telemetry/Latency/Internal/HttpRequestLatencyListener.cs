// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Telemetry.Latency.Internal;

internal sealed class HttpRequestLatencyListener : EventListener
{
    private const string SocketProviderName = "System.Net.Sockets";
    private const string HttpProviderName = "System.Net.Http";
    private const string NameResolutionProivderName = "System.Net.NameResolution";

    private readonly ConcurrentDictionary<string, EventSource?> _eventSources = new()
    {
        [SocketProviderName] = null,
        [HttpProviderName] = null,
        [NameResolutionProivderName] = null
    };

    internal HttpClientLatencyContext LatencyContext { get; }

    private readonly EventToCheckpointToken _eventToCheckpointToken;

    private int _enabled;

    internal bool Enabled => _enabled == 1;

    public HttpRequestLatencyListener(HttpClientLatencyContext latencyContext, ILatencyContextTokenIssuer tokenIssuer)
    {
        LatencyContext = latencyContext;
        _eventToCheckpointToken = new(tokenIssuer);
    }

    public void Enable()
    {
        if (Interlocked.CompareExchange(ref _enabled, 1, 0) == 0)
        {
            foreach (var eventSource in _eventSources)
            {
                if (eventSource.Value != null)
                {
                    EnableEventSource(eventSource.Value);
                }
            }
        }
    }

    internal void OnEventWritten(string eventSourceName, string? eventName)
    {
        // If event of interest, add a checkpoint for it.
        CheckpointToken? token = _eventToCheckpointToken.GetCheckpointToken(eventSourceName, eventName);
        if (token.HasValue)
        {
            LatencyContext.Get()?.AddCheckpoint(token.Value);
        }
    }

    internal void OnEventSourceCreated(string eventSourceName, EventSource eventSource)
    {
        if (_eventSources.ContainsKey(eventSourceName))
        {
            _eventSources[eventSourceName] = eventSource;
            EnableEventSource(eventSource);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        OnEventSourceCreated(eventSource.Name, eventSource);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        OnEventWritten(eventData.EventSource.Name, eventData.EventName);
    }

    private void EnableEventSource(EventSource eventSource)
    {
        if (Enabled && !eventSource.IsEnabled())
        {
            EnableEvents(eventSource, EventLevel.Informational);
        }
    }

    private sealed class EventToCheckpointToken
    {
        private static readonly Dictionary<string, string> _socketMap = new()
        {
            { "ConnectStart", HttpCheckpoints.SocketConnectStart },
            { "ConnectStop", HttpCheckpoints.SocketConnectEnd }
        };

        private static readonly Dictionary<string, string> _nameResolutionMap = new()
        {
            { "ResolutionStart", HttpCheckpoints.NameResolutionStart },
            { "ResolutionStop", HttpCheckpoints.NameResolutionEnd }
        };

        private static readonly Dictionary<string, string> _httpMap = new()
        {
            { "ConnectionEstablished", HttpCheckpoints.ConnectionEstablished },
            { "RequestLeftQueue", HttpCheckpoints.RequestLeftQueue },
            { "RequestHeadersStart", HttpCheckpoints.RequestHeadersStart },
            { "RequestHeadersStop", HttpCheckpoints.RequestHeadersEnd },
            { "RequestContentStart", HttpCheckpoints.RequestContentStart },
            { "RequestContentStop", HttpCheckpoints.RequestContentEnd },
            { "ResponseHeadersStart", HttpCheckpoints.ResponseHeadersStart },
            { "ResponseHeadersStop", HttpCheckpoints.ResponseHeadersEnd },
            { "ResponseContentStart", HttpCheckpoints.ResponseContentStart },
            { "ResponseContentStop", HttpCheckpoints.ResponseContentEnd }
        };

        private readonly FrozenDictionary<string, FrozenDictionary<string, CheckpointToken>> _eventToTokenMap;

        public EventToCheckpointToken(ILatencyContextTokenIssuer tokenIssuer)
        {
            Dictionary<string, CheckpointToken> socket = [];
            foreach (string key in _socketMap.Keys)
            {
                socket[key] = tokenIssuer.GetCheckpointToken(_socketMap[key]);
            }

            Dictionary<string, CheckpointToken> nameResolution = [];
            foreach (string key in _nameResolutionMap.Keys)
            {
                nameResolution[key] = tokenIssuer.GetCheckpointToken(_nameResolutionMap[key]);
            }

            Dictionary<string, CheckpointToken> http = [];
            foreach (string key in _httpMap.Keys)
            {
                http[key] = tokenIssuer.GetCheckpointToken(_httpMap[key]);
            }

            _eventToTokenMap = new Dictionary<string, FrozenDictionary<string, CheckpointToken>>
            {
                { SocketProviderName, socket.ToFrozenDictionary(StringComparer.Ordinal) },
                { NameResolutionProivderName, nameResolution.ToFrozenDictionary(StringComparer.Ordinal) },
                { HttpProviderName, http.ToFrozenDictionary(StringComparer.Ordinal) }
            }.ToFrozenDictionary(StringComparer.Ordinal);
        }

        public CheckpointToken? GetCheckpointToken(string eventSourceName, string? eventName)
        {
            if (eventName != null && _eventToTokenMap.TryGetValue(eventSourceName, out var events))
            {
                if (events.TryGetValue(eventName, out var token))
                {
                    return token;
                }
            }

            return null;
        }
    }
}
