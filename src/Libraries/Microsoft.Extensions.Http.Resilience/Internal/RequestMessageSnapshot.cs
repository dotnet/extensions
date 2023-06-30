// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class RequestMessageSnapshot : IResettable, IDisposable
{
    private static readonly ObjectPool<RequestMessageSnapshot> _snapshots = PoolFactory.CreateResettingPool<RequestMessageSnapshot>();

    private readonly List<KeyValuePair<string, IEnumerable<string>>> _headers = new();
    private readonly List<KeyValuePair<string, object?>> _properties = new();

    private HttpMethod? _method;
    private Uri? _requestUri;
    private Version? _version;
    private HttpContent? _content;

    public static RequestMessageSnapshot Create(HttpRequestMessage request)
    {
        var snapshot = _snapshots.Get();
        snapshot.Initialize(request);
        return snapshot;
    }

    public HttpRequestMessage CreateRequestMessage()
    {
        var clone = new HttpRequestMessage(_method!, _requestUri)
        {
            Content = _content,
            Version = _version!
        };

#if NET5_0_OR_GREATER
        foreach (var prop in _properties)
        {
            _ = clone.Options.TryAdd(prop.Key, prop.Value);
        }
#else
        foreach (var prop in _properties)
        {
            clone.Properties.Add(prop);
        }
#endif
        foreach (KeyValuePair<string, IEnumerable<string>> header in _headers)
        {
            _ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    bool IResettable.TryReset()
    {
        _properties.Clear();
        _headers.Clear();

        _method = null;
        _version = null;
        _requestUri = null;
        _content = null;

        return true;
    }

    void IDisposable.Dispose() => _snapshots.Return(this);

    private void Initialize(HttpRequestMessage request)
    {
        if (request.Content is StreamContent)
        {
            Throw.InvalidOperationException($"{nameof(StreamContent)} content cannot by cloned.");
        }

        _method = request.Method;
        _version = request.Version;
        _requestUri = request.RequestUri;
        _content = request.Content;

        // headers
        _headers.AddRange(request.Headers);

        // props
#if NET5_0_OR_GREATER
        _properties.AddRange(request.Options);
#else
        _properties.AddRange(request.Properties);
#endif
    }
}
