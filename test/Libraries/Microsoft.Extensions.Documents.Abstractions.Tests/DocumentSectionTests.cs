// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentSectionTests
{
    [Fact]
    public void StoresMarkdownAndStartsWithEmptyElements()
    {
        DocumentSection section = new("stored section markdown");

        Assert.Equal("stored section markdown", section.StoredMarkdown);
        Assert.Empty(section.Elements);
        Assert.Null(section.ParentSection);
        Assert.Null(section.Address);
    }

    [Fact]
    public void ToMarkdown_ReturnsStoredMarkdownWhenSectionIsEmpty()
    {
        DocumentSection section = new("stored section markdown");

        Assert.Equal("stored section markdown", section.ToMarkdown());
    }

    [Fact]
    public void NestedSections_AreEnumeratedDepthFirst()
    {
        using Document document = new("document");
        DocumentSection root = new("root");
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentSection nested = new("nested");
        DocumentTextElement nestedFirst = new(DocumentTextKind.Paragraph, "nested-first");
        DocumentTextElement nestedLast = new(DocumentTextKind.Paragraph, "nested-last");
        DocumentTextElement last = new(DocumentTextKind.Paragraph, "last");

        nested.Elements.Add(nestedFirst);
        nested.Elements.Add(nestedLast);
        root.Elements.Add(first);
        root.Elements.Add(nested);
        root.Elements.Add(last);
        document.Sections.Add(root);

        DocumentElement[] content = document.EnumerateContent().ToArray();

        Assert.Equal(new DocumentElement[] { first, nestedFirst, nestedLast, last }, content);
        Assert.DoesNotContain(nested, content);
    }

    [Fact]
    public void SectionCollection_RejectsDuplicateSectionOwnership()
    {
        using Document firstDocument = new("first-document");
        using Document secondDocument = new("second-document");
        DocumentSection section = new("section");

        firstDocument.Sections.Add(section);

        Assert.Throws<InvalidOperationException>(() => secondDocument.Sections.Add(section));

        Assert.Single(firstDocument.Sections);
        Assert.Empty(secondDocument.Sections);
        Assert.Equal("s0", section.Address);
    }

    [Fact]
    public void SectionCollection_SetRemoveAndClear_UpdateOwnership()
    {
        using Document document = new("document");
        DocumentSection oldSection = new("old");
        DocumentTextElement oldContent = new(DocumentTextKind.Paragraph, "old content");
        oldSection.Elements.Add(oldContent);
        DocumentSection secondSection = new("second");
        DocumentSection replacement = new("replacement");
        DocumentTextElement replacementContent = new(DocumentTextKind.Paragraph, "replacement content");
        replacement.Elements.Add(replacementContent);

        document.Sections.Add(oldSection);
        document.Sections.Add(secondSection);
        document.Sections[0] = replacement;

        Assert.Null(oldSection.Address);
        Assert.Null(oldContent.Address);
        Assert.Equal("s0", replacement.Address);
        Assert.Equal("s0/e0", replacementContent.Address);

        document.Sections.Remove(replacement);

        Assert.Null(replacement.Address);
        Assert.Null(replacementContent.Address);
        Assert.Equal("s0", secondSection.Address);

        document.Sections.Clear();

        Assert.Empty(document.Sections);
        Assert.Null(secondSection.Address);
    }
}
