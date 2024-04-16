// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class RequestMessageSnapshot : IResettable, IDisposable
{
    private static readonly ObjectPool<RequestMessageSnapshot> _snapshots = PoolFactory.CreateResettingPool<RequestMessageSnapshot>();

    private readonly List<KeyValuePair<string, IEnumerable<string>>> _headers = [];
    private readonly List<KeyValuePair<string, object?>> _properties = [];

    private HttpMethod? _method;
    private Uri? _requestUri;
    private Version? _version;
    private HttpContent? _content;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "Past the point of no cancellation.")]
    public static async Task<RequestMessageSnapshot> CreateAsync(HttpRequestMessage request)
    {
        _ = Throw.IfNull(request);

        var snapshot = _snapshots.Get();
        await snapshot.InitializeAsync(request).ConfigureAwait(false);
        return snapshot;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "Past the point of no cancellation.")]
    public async Task<HttpRequestMessage> CreateRequestMessageAsync()
    {
        if (IsReset())
        {
            throw new InvalidOperationException($"{nameof(CreateRequestMessageAsync)}() cannot be called on a snapshot object that has been reset and has not been initialized");
        }

        var clone = new HttpRequestMessage(_method!, _requestUri)
        {
            Version = _version!
        };

        if (_content is StreamContent)
        {
            (HttpContent? content, HttpContent? clonedContent) = await CloneContentAsync(_content).ConfigureAwait(false);
            _content = content;
            clone.Content = clonedContent;
        }
        else
        {
            clone.Content = _content;
        }

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Bug", "S2952:Classes should \"Dispose\" of members from the classes' own \"Dispose\" methods", Justification = "Handled by ObjectPool")]
    bool IResettable.TryReset()
    {
        _properties.Clear();
        _headers.Clear();

        _method = null;
        _version = null;
        _requestUri = null;
        if (_content is StreamContent)
        {
            // a snapshot's StreamContent is always a unique copy (deep clone)
            // therefore, it is safe to dispose when snapshot is no longer needed
            _content.Dispose();
        }

        _content = null;

        return true;
    }

    void IDisposable.Dispose() => _snapshots.Return(this);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "Past the point of no cancellation.")]
    private static async Task<(HttpContent? content, HttpContent? clonedContent)> CloneContentAsync(HttpContent? content)
    {
        HttpContent? clonedContent = null;
        if (content != null)
        {
            HttpContent originalContent = content;
            Stream originalRequestBody = await content.ReadAsStreamAsync().ConfigureAwait(false);
            MemoryStream clonedRequestBody = new MemoryStream();
            await originalRequestBody.CopyToAsync(clonedRequestBody).ConfigureAwait(false);
            clonedRequestBody.Position = 0;
            if (originalRequestBody.CanSeek)
            {
                originalRequestBody.Position = 0;
            }
            else
            {
                originalRequestBody = new MemoryStream();
                await clonedRequestBody.CopyToAsync(originalRequestBody).ConfigureAwait(false);
                originalRequestBody.Position = 0;
                clonedRequestBody.Position = 0;
            }

            clonedContent = new StreamContent(clonedRequestBody);
            content = new StreamContent(originalRequestBody);
            foreach (KeyValuePair<string, IEnumerable<string>> header in originalContent.Headers)
            {
                _ = clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                _ = content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return (content, clonedContent);
    }

    private bool IsReset()
    {
        return _method == null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Resilience", "EA0014:The async method doesn't support cancellation", Justification = "Past the point of no cancellation.")]
    private async Task InitializeAsync(HttpRequestMessage request)
    {
        _method = request.Method;
        _version = request.Version;
        _requestUri = request.RequestUri;
        if (request.Content is StreamContent)
        {
            (HttpContent? requestContent, HttpContent? clonedRequestContent) = await CloneContentAsync(request.Content).ConfigureAwait(false);
            _content = clonedRequestContent;
            request.Content = requestContent;
        }
        else
        {
            _content = request.Content;
        }

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
