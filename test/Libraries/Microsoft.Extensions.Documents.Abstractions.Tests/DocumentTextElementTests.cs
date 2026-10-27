// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTextElementTests
{
    [Fact]
    public void StoresRoleMarkdownAndText()
    {
        DocumentTextElement element = new("stored markdown", "plain text", "pageFooter");

        Assert.Equal("pageFooter", element.Role);
        Assert.Equal("stored markdown", element.Markdown);
        Assert.Equal("stored markdown", element.ToMarkdown());
        Assert.Equal("plain text", element.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("heading")]
    [InlineData("pageHeader")]
    [InlineData("pageFooter")]
    [InlineData("vendor:caption")]
    [InlineData("HeAdInG")]
    [InlineData(" Custom Role ")]
    public void Role_NullKnownAndCustomValuesPreserveTextSourcesAndProjection(string? role)
    {
        using Document document = new("role", "supplied root");
        DocumentSection section = new("supplied section");
        DocumentTextElement element = new("**citation** [1]", "citation [1]", role);
        DocumentSourcePage page = new("citation-page", 7);
        DocumentSourceReference citation = new(page, DocumentAnnotationTestData.Polygon());
        element.Sources.Add(citation);
        section.Elements.Add(element);
        document.Sections.Add(section);

        Assert.Equal(role, element.Role);
        Assert.Null(element.HeadingLevel);
        Assert.Equal("**citation** [1]", element.Markdown);
        Assert.Equal("citation [1]", element.Text);
        Assert.Equal("**citation** [1]", element.ToMarkdown());
        Assert.Equal("**citation** [1]", document.ToMarkdown());
        Assert.Equal("supplied root", document.Markdown);
        Assert.Same(citation, Assert.Single(element.Sources));
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Equal(DocumentAnnotationTestData.Polygon(), citation.Polygon);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Role_EmptyOrWhitespaceRejected(string role)
    {
        Assert.Throws<ArgumentException>(nameof(role), () => new DocumentTextElement("markdown", role: role));
    }

    [Fact]
    public void HeadingAndPageHeader_AreDistinguishedByRoleWithoutChangingProjection()
    {
        DocumentTextElement heading = new("Title", role: "heading") { HeadingLevel = 1 };
        DocumentTextElement pageHeader = new("Title", role: "pageHeader");
        DocumentTextElement custom = new("ordinary **text**", role: "VendorHeading") { HeadingLevel = 10 };

        Assert.Equal("heading", heading.Role);
        Assert.Equal("pageHeader", pageHeader.Role);
        Assert.NotEqual(heading.Role, pageHeader.Role);
        Assert.Equal(1, heading.HeadingLevel);
        Assert.Null(pageHeader.HeadingLevel);
        Assert.Equal("Title", heading.ToMarkdown());
        Assert.Equal("Title", pageHeader.ToMarkdown());
        Assert.Equal("VendorHeading", custom.Role);
        Assert.Equal(10, custom.HeadingLevel);
        Assert.Equal("ordinary **text**", custom.ToMarkdown());
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
    public void HeadingLevel_AcceptsOneThroughTen(int level)
    {
        DocumentTextElement element = new("header", role: "heading")
        {
            HeadingLevel = level
        };

        Assert.Equal(level, element.HeadingLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void HeadingLevel_RejectsZeroAndEleven(int level)
    {
        DocumentTextElement element = new("header", role: "heading")
        {
            HeadingLevel = 5
        };

        Assert.Throws<ArgumentOutOfRangeException>("value", () => element.HeadingLevel = level);
        Assert.Equal(5, element.HeadingLevel);
    }

    [Fact]
    public void HeadingLevel_SameValue_DoesNotChangeAttachedVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new("header", role: "heading")
        {
            HeadingLevel = 3
        };
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        element.HeadingLevel = 3;

        Assert.Equal(version, document.Version);
        element.HeadingLevel = null;
        Assert.Null(element.HeadingLevel);
        Assert.Equal(version + 1, document.Version);
        Assert.Equal("heading", element.Role);
        Assert.Equal("header", document.ToMarkdown());
    }
}
