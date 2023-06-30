// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
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
        var fallbackUri = new Uri("https://fallback-uri.com");
        var initialUri = new Uri(initialUriString);

        initialUri.ReplaceHost(fallbackUri).Should().Be(new Uri(expectedUriString));
    }

    [Theory]
    [InlineData("https://initial.uri", "https://initial.uri")]
    [InlineData("https://initial.uri/somepath", "https://initial.uri/somepath")]
    [InlineData("https://initial.uri/somepath/someotherpath", "https://initial.uri/somepath/someotherpath")]
    [InlineData("https://initial.uri:2030/somepath", "https://initial.uri:2030/somepath")]
    [InlineData("https://initial.uri:2030/somepath?query=value", "https://initial.uri:2030/somepath?query=value")]
    [InlineData("https://initial.uri?a=1&b=2&c=3", "https://initial.uri?a=1&b=2&c=3")]
    [InlineData("https://initial.uri?", "https://initial.uri")]
    public void ReplaceHost_TargetHostSame_ShouldReturnInitialUri(string initialUriString, string replacementUri)
    {
        var initialUri = new Uri(initialUriString);

        initialUri.ReplaceHost(new Uri(replacementUri)).Should().BeSameAs(initialUri);
    }
}
