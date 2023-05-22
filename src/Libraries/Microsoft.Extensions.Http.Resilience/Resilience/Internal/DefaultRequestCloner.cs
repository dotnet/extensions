// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Default implementation of <see cref="IRequestClonerInternal"/> interface for cloning requests.
/// </summary>
/// <remarks>
/// The request content is only copied, not deeply cloned.
/// If the request is cloned outside the <see cref="HttpClient"/> middlewares, the content must be cloned as well.
/// </remarks>
internal sealed class DefaultRequestCloner : IRequestClonerInternal
{
    public IHttpRequestMessageSnapshot CreateSnapshot(HttpRequestMessage request)
    {
        _ = Throw.IfNull(request);

        return new Snapshot(request);
    }

    private sealed class Snapshot : IHttpRequestMessageSnapshot
    {
        private static readonly ObjectPool<List<KeyValuePair<string, IEnumerable<string>>>> _headersPool = PoolFactory.CreateListPool<KeyValuePair<string, IEnumerable<string>>>();
        private static readonly ObjectPool<List<KeyValuePair<string, object?>>> _propertiesPool = PoolFactory.CreateListPool<KeyValuePair<string, object?>>();

        private readonly HttpMethod _method;
        private readonly Uri? _requestUri;
        private readonly Version _version;
        private readonly HttpContent? _content;
        private readonly List<KeyValuePair<string, IEnumerable<string>>> _headers;
        private readonly List<KeyValuePair<string, object?>> _properties;

        public Snapshot(HttpRequestMessage request)
        {
            if (request.Content is StreamContent)
            {
                Throw.InvalidOperationException($"{nameof(StreamContent)} content cannot by cloned using the {nameof(DefaultRequestCloner)}.");
            }

            _method = request.Method;
            _version = request.Version;
            _requestUri = request.RequestUri;
            _content = request.Content;

            // headers
            _headers = _headersPool.Get();
            _headers.AddRange(request.Headers);

            // props
            _properties = _propertiesPool.Get();
#if NET5_0_OR_GREATER
            _properties.AddRange(request.Options);
#else
            _properties.AddRange(request.Properties);
#endif
        }

        public HttpRequestMessage Create()
        {
            var clone = new HttpRequestMessage(_method, _requestUri)
            {
                Content = _content,
                Version = _version
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

        public void Dispose()
        {
            _propertiesPool.Return(_properties);
            _headersPool.Return(_headers);
        }
    }
}
