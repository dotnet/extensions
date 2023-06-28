// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

public class RequestMessageSnapshotTests
{
    [Fact]
    public void CreateSnapshot_StreamContent_ShouldThrow()
    {
        var initialRequest = new HttpRequestMessage
        {
            RequestUri = new Uri("https://dummy-uri.com?query=param"),
            Method = HttpMethod.Get,
            Content = new StreamContent(new MemoryStream())
        };


        var exception = Assert.Throws<InvalidOperationException>(() => RequestMessageSnapshot.Create(initialRequest));
        Assert.Equal("StreamContent content cannot by cloned.", exception.Message);
        initialRequest.Dispose();
    }

    [Fact]
    public void CreateSnapshot_CreatesClone()
    {
        using var request = CreateRequest();
        using var snapshot = RequestMessageSnapshot.Create(request);
        var cloned = snapshot.CreateRequestMessage();
        AssertClonedMessage(request, cloned);
    }

    [Fact]
    public void CreateSnapshot_OriginalMessageChanged_SnapshotReturnsOriginalData()
    {
        using var request = CreateRequest();
        using var snapshot = RequestMessageSnapshot.Create(request);

        request.Properties["some-new-prop"] = "ABC";
        var cloned = snapshot.CreateRequestMessage();
        cloned.Properties.Should().NotContainKey("some-new-prop");
    }

    private static HttpRequestMessage CreateRequest()
    {
        var initialRequest = new HttpRequestMessage
        {
            RequestUri = new Uri("https://dummy-uri.com?query=param"),
            Method = HttpMethod.Get,
            Version = new Version(1, 1),
            Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
        };

        initialRequest.Headers.Add("Authorization", "Bearer token");
        initialRequest.Properties.Add("A", "A");
        initialRequest.Properties.Add("B", "B");

#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET5_0_OR_GREATER we don't build .NET 5.0,
        // and as such the API is available in .NET 8 onwards.
        initialRequest.Options.Set(new HttpRequestOptionsKey<string>("C"), "C");
        initialRequest.Options.Set(new HttpRequestOptionsKey<string>("D"), "D");
#endif
        return initialRequest;
    }

    private static void AssertClonedMessage(HttpRequestMessage initialRequest, HttpRequestMessage cloned)
    {
        Assert.NotNull(cloned);
        Assert.Equal(initialRequest.Method, cloned.Method);
        Assert.Equal(initialRequest.RequestUri, cloned.RequestUri);
        Assert.Equal(initialRequest.Content, cloned.Content);
        Assert.Equal(initialRequest.Version, cloned.Version);

        Assert.NotNull(cloned.Headers.Authorization);

        cloned.Properties["A"].Should().Be("A");
        cloned.Properties["B"].Should().Be("B");

#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET5_0_OR_GREATER we don't build .NET 5.0,
        // and as such the API is available in .NET 8 onwards.
        initialRequest.Options.TryGetValue(new HttpRequestOptionsKey<string>("C"), out var val).Should().BeTrue();
        val.Should().Be("C");

        initialRequest.Options.TryGetValue(new HttpRequestOptionsKey<string>("D"), out val).Should().BeTrue();
        val.Should().Be("D");
#endif
    }
}
