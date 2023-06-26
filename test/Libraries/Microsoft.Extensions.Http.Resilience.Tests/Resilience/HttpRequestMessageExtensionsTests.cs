// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

#pragma warning disable CA2000 // Test class

public class HttpRequestMessageExtensionsTests
{
    [Theory]
    [InlineData("https://initial.uri", "https://fallback-uri.com")]
    [InlineData("https://initial.uri/somepath", "https://fallback-uri.com/somepath")]
    [InlineData("https://initial.uri/somepath/someotherpath", "https://fallback-uri.com/somepath/someotherpath")]
    [InlineData("https://initial.uri:2030/somepath", "https://fallback-uri.com/somepath")]
    [InlineData("https://initial.uri:2030/somepath?query=value", "https://fallback-uri.com/somepath?query=value")]
    [InlineData("https://initial.uri?a=1&b=2&c=3", "https://fallback-uri.com?a=1&b=2&c=3")]
    [InlineData("https://initial.uri?", "https://fallback-uri.com")]
    public void ReplaceHost_ValidArguments_ShouldReplaceUri(string initialUriString, string expectedUriString)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(initialUriString));
        var fallbackUri = new Uri("https://fallback-uri.com");

        request = request.ReplaceHost(fallbackUri);
        var expectedUri = new Uri(expectedUriString);
        Assert.Equal(expectedUri, request.RequestUri);
    }

    [Fact]
    public void ReplaceHost_NullUri_ShouldThrow()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://initial.uri"));
        Assert.Throws<ArgumentNullException>(() =>
            request.ReplaceHost(null!));
    }
}
