// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

#pragma warning disable CA2000 // Test class

public class UriExtensionsTests
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
    [InlineData("https://initial.uri", "https://initial.uri", true)]
    [InlineData("https://initial.uri:123", "https://initial.uri:123", true)]
    [InlineData("http://initial.uri:123", "http://initial.uri:123", true)]
    [InlineData("https://initial.uri", "https://initial.uri:123", false)]
    [InlineData("https://initial.uri:123", "https://initial.uri:123/some-path", false)]
    [InlineData("http://initial.uri:123", "https://initial.uri:123", false)]
    public void ReplaceHost_TargetHostSame_ShouldReturnInitialUri(string initialUriString, string replacementUri, bool shouldBeSame)
    {
        var initialUri = new Uri(initialUriString);

        if (shouldBeSame)
        {
            initialUri.ReplaceHost(new Uri(replacementUri)).Should().BeSameAs(initialUri);
        }
        else
        {
            initialUri.ReplaceHost(new Uri(replacementUri)).Should().NotBeSameAs(initialUri);
        }
    }
}
