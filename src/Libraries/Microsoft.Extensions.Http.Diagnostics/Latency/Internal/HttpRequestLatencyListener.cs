// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

internal sealed class HttpRequestLatencyListener : EventListener
{
    private const string SocketProviderName = "System.Net.Sockets";
    private const string HttpProviderName = "System.Net.Http";
    private const string NameResolutionProivderName = "System.Net.NameResolution";

    private readonly FrozenDictionary<string, FrozenDictionary<string, CheckpointToken>> _eventToTokenMap;

    internal HttpClientLatencyContext LatencyContext { get; }

    private int _enabled;

    internal bool Enabled => _enabled == 1;

    public HttpRequestLatencyListener(HttpClientLatencyContext latencyContext, ILatencyContextTokenIssuer tokenIssuer)
    {
        LatencyContext = latencyContext;
        _eventToTokenMap = EventToCheckpointToken.Build(tokenIssuer);
    }

    public void Enable()
    {
        if (Interlocked.CompareExchange(ref _enabled, 1, 0) == 0)
        {
#if NETSTANDARD
            foreach (var eventSource in EventSource.GetSources())
            {
                OnEventSourceCreated(eventSource.Name, eventSource);
            }
#else
            // process already existing listeners once again
            EventSourceCreated += (_, args) => OnEventSourceCreated(args.EventSource!);
#endif

        }
    }

    internal void OnEventWritten(string eventSourceName, string? eventName)
    {
        // If event of interest, add a checkpoint for it.
        if (eventName != null && _eventToTokenMap[eventSourceName].TryGetValue(eventName, out var token))
        {
            LatencyContext.Get()?.AddCheckpoint(token);
        }
    }

    internal void OnEventSourceCreated(string eventSourceName, EventSource eventSource)
    {
        if (Enabled && _eventToTokenMap.ContainsKey(eventSourceName))
        {
            EnableEvents(eventSource, EventLevel.Informational);
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

    private static class EventToCheckpointToken
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

        public static FrozenDictionary<string, FrozenDictionary<string, CheckpointToken>> Build(ILatencyContextTokenIssuer tokenIssuer)
        {
            Dictionary<string, CheckpointToken> socket = [];
            foreach (var kv in _socketMap)
            {
                socket[kv.Key] = tokenIssuer.GetCheckpointToken(kv.Value);
            }

            Dictionary<string, CheckpointToken> nameResolution = [];
            foreach (var kv in _nameResolutionMap)
            {
                nameResolution[kv.Key] = tokenIssuer.GetCheckpointToken(kv.Value);
            }

            Dictionary<string, CheckpointToken> http = [];
            foreach (var kv in _httpMap)
            {
                http[kv.Key] = tokenIssuer.GetCheckpointToken(kv.Value);
            }

            return new Dictionary<string, FrozenDictionary<string, CheckpointToken>>
            {
                { SocketProviderName, socket.ToFrozenDictionary() },
                { NameResolutionProivderName, nameResolution.ToFrozenDictionary() },
                { HttpProviderName, http.ToFrozenDictionary() }
            }.ToFrozenDictionary();
        }
    }
}
