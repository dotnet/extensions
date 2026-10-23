// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTextElementTests
{
    [Fact]
    public void StoresKindMarkdownAndText()
    {
        DocumentTextElement element = new(DocumentTextKind.Footer, "stored markdown", "plain text");

        Assert.Equal(DocumentTextKind.Footer, element.Kind);
        Assert.Equal("stored markdown", element.StoredMarkdown);
        Assert.Equal("stored markdown", element.GetMarkdown());
        Assert.Equal("plain text", element.Text);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void HeaderLevel_AcceptsOneThroughTen(int level)
    {
        DocumentTextElement element = new(DocumentTextKind.Header, "header")
        {
            HeaderLevel = level
        };

        Assert.Equal(level, element.HeaderLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void HeaderLevel_RejectsZeroAndEleven(int level)
    {
        DocumentTextElement element = new(DocumentTextKind.Header, "header")
        {
            HeaderLevel = 5
        };

        Assert.Throws<ArgumentOutOfRangeException>("value", () => element.HeaderLevel = level);
        Assert.Equal(5, element.HeaderLevel);
    }

    [Fact]
    public void HeaderLevel_SameValue_DoesNotChangeAttachedVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Header, "header")
        {
            HeaderLevel = 3
        };
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        element.HeaderLevel = 3;

        Assert.Equal(version, document.Version);
    }
}
