// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class RequestMessageSnapshotTests : IDisposable
{
    private readonly Uri _requestUri = new Uri("https://www.example.com/some-resource");
    private bool _disposedValue;
    private HttpRequestMessage? _requestMessage;

    public RequestMessageSnapshotTests()
    {
        _requestMessage = new HttpRequestMessage(HttpMethod.Post, _requestUri);
    }

    [Fact]
    public async Task CreateSnapshotAsync_RequestMessageContainsStringContent_Success()
    {
        _requestMessage!.Content = new StringContent("some string content");
        AddRequestHeaders(_requestMessage);
        AddRequestOptions(_requestMessage);
        AddContentHeaders(_requestMessage!.Content);
        using RequestMessageSnapshot snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage).ConfigureAwait(false);
        using HttpRequestMessage clonedRequestMessage = await snapshot.CreateRequestMessageAsync().ConfigureAwait(false);
        await AssertRequestMessagesAreEqual(_requestMessage, clonedRequestMessage).ConfigureAwait(false);
    }

    [Fact]
    public async Task CreateSnapshotAsync_RequestMessageContainsStreamContent_Success()
    {
        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync("some stream content").ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        _requestMessage!.Content = new StreamContent(stream);
        AddRequestHeaders(_requestMessage);
        AddRequestOptions(_requestMessage);
        AddContentHeaders(_requestMessage!.Content);
        using RequestMessageSnapshot snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage).ConfigureAwait(false);
        using HttpRequestMessage clonedRequestMessage = await snapshot.CreateRequestMessageAsync().ConfigureAwait(false);
        await AssertRequestMessagesAreEqual(_requestMessage, clonedRequestMessage).ConfigureAwait(false);
    }

    [Fact]
    public async Task CreateSnapshotAsync_RequestMessageContainsNonSeekableStreamContent_Success()
    {
        using var stream = new NonSeekableStream("some stream content");
        _requestMessage!.Content = new StreamContent(stream);
        AddRequestHeaders(_requestMessage);
        AddRequestOptions(_requestMessage);
        AddContentHeaders(_requestMessage!.Content);
        using RequestMessageSnapshot snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage).ConfigureAwait(false);
        using HttpRequestMessage clonedRequestMessage = await snapshot.CreateRequestMessageAsync().ConfigureAwait(false);
        await AssertRequestMessagesAreEqual(_requestMessage, clonedRequestMessage).ConfigureAwait(false);
    }

    [Fact]
    public async Task CreateSnapshotAsync_RequestMessageHasNoContent_Success()
    {
        _requestMessage!.Method = HttpMethod.Get;
        Assert.Null(_requestMessage!.Content);
        AddRequestHeaders(_requestMessage);
        AddRequestOptions(_requestMessage);
        using RequestMessageSnapshot snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage).ConfigureAwait(false);
        using HttpRequestMessage clonedRequestMessage = await snapshot.CreateRequestMessageAsync().ConfigureAwait(false);
        await AssertRequestMessagesAreEqual(_requestMessage, clonedRequestMessage).ConfigureAwait(false);
    }

    [Fact]
    public async Task CreateSnapshotAsync_RequestMessageIsNull_ThrowsException()
    {
        HttpRequestMessage? requestMessage = null;
#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await RequestMessageSnapshot.CreateAsync(requestMessage).ConfigureAwait(false)).ConfigureAwait(false);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public async Task CreateSnapshotAsync_OriginalMessageChanged_SnapshotReturnsOriginalData()
    {
        using var snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage!).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        _requestMessage!.Options.Set(new HttpRequestOptionsKey<object?>("Some.New.Request.Option"), "some new request option value");
#else
        _requestMessage!.Properties["Some.New.Request.Property"] = "some new request property value";
#endif

        var cloned = await snapshot.CreateRequestMessageAsync().ConfigureAwait(false);

#if NET5_0_OR_GREATER
        cloned.Options.Should().NotContainKey("Some.New.Request.Option");
#else
        cloned.Properties.Should().NotContainKey("Some.New.Request.Property");
#endif
    }

    [Fact]
    public async Task CreateRequestMessageAsync_SnapshotIsReset_ThrowsException()
    {
        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync("some stream content").ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        _requestMessage!.Content = new StreamContent(stream);
        AddRequestHeaders(_requestMessage);
        AddRequestOptions(_requestMessage);
        AddContentHeaders(_requestMessage!.Content);
        using RequestMessageSnapshot snapshot = await RequestMessageSnapshot.CreateAsync(_requestMessage).ConfigureAwait(false);
        ((IResettable)snapshot).TryReset();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(snapshot.CreateRequestMessageAsync);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _requestMessage!.Dispose();
                _requestMessage = null;
            }

            _disposedValue = true;
        }
    }

    private static void AddContentHeaders(HttpContent content)
    {
        content.Headers.TryAddWithoutValidation("Some-Content-Header", "some content header value");
        content.Headers.TryAddWithoutValidation("Some-Other-Content-Header", "some other content header value");
    }

    private static void AddRequestHeaders(HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.TryAddWithoutValidation("Some-Header", "some header value");
        requestMessage.Headers.TryAddWithoutValidation("Some-Other-Header", "some other header value");
    }

    private static void AddRequestOptions(HttpRequestMessage requestMessage)
    {
#if NET5_0_OR_GREATER
        requestMessage.Options.TryAdd("Some.Request.Option", "some request option value");
        requestMessage.Options.TryAdd("Some.Other.Request.Option", "some other request option value");
#else
        requestMessage.Properties["Some.Request.Property"] = "some request property value";
        requestMessage.Properties["Some.Other.Request.Property"] = "some other request property value";
#endif
    }

    private static async Task AssertRequestMessagesAreEqual(HttpRequestMessage requestMessageA, HttpRequestMessage requestMessageB)
    {
        Assert.NotNull(requestMessageA);
        Assert.NotNull(requestMessageB);
        Assert.NotSame(requestMessageA, requestMessageB); // assert no shallow copy
        Assert.Equal(requestMessageA.Method, requestMessageB.Method);
        Assert.Equal(requestMessageA.RequestUri?.AbsoluteUri, requestMessageB.RequestUri?.AbsoluteUri);
        Assert.Equal(requestMessageA.Version, requestMessageB.Version);
        if (requestMessageA.Content == null)
        {
            Assert.Null(requestMessageB.Content);
        }
        else if (requestMessageB.Content == null)
        {
            Assert.Null(requestMessageA.Content);
        }
        else
        {
            if (requestMessageA.Content is StreamContent)
            {
                Assert.NotSame(requestMessageA.Content, requestMessageB.Content); // assert no shallow copy
            }
            else
            {
                Assert.Same(requestMessageA.Content, requestMessageB.Content); // assert shallow copy
            }

            Assert.Equal(
                await requestMessageA.Content.ReadAsStringAsync().ConfigureAwait(false),
                await requestMessageB.Content.ReadAsStringAsync().ConfigureAwait(false));

            foreach (KeyValuePair<string, IEnumerable<string>> header in requestMessageA.Content.Headers)
            {
                Assert.Contains(requestMessageB.Content.Headers, (x) => x.Key == header.Key && x.Value.Any((y) => header.Value.Any((z) => z == y)));
            }
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in requestMessageA.Headers)
        {
            Assert.NotSame(requestMessageA.Headers, requestMessageB.Headers); // assert no shallow copy
            Assert.Contains(requestMessageB.Headers, (x) => x.Key == header.Key
                && x.Value.Any((y) => header.Value.Any((z) => z == y))
                && x.Value != header.Value); // assert no shallow copy
        }

#if NET5_0_OR_GREATER
        foreach (KeyValuePair<string, object?> option in requestMessageA.Options)
        {
            Assert.NotSame(requestMessageA.Options, requestMessageB.Options); // assert no shallow copy
            Assert.Contains(requestMessageB.Options, (x) => x.Key == option.Key && x.Value == option.Value);
        }
#else
        foreach (KeyValuePair<string, object?> property in requestMessageA.Properties)
        {
            Assert.NotSame(requestMessageA.Properties, requestMessageB.Properties); // assert no shallow copy
            Assert.Contains(requestMessageB.Properties, (x) => x.Key == property.Key && x.Value == property.Value);
        }
#endif
    }

    private class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(string str)
            : base(Encoding.UTF8.GetBytes(str), writable: false)
        {
        }

        public override bool CanSeek => false;
    }
}
