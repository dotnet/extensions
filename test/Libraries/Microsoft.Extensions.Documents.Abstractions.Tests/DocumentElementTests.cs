// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentElementTests
{
    [Fact]
    public void LocalId_IsDistinctPerInstance()
    {
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement second = new(DocumentTextKind.Paragraph, "second");

        Assert.NotEqual(Guid.Empty, first.LocalId);
        Assert.NotEqual(Guid.Empty, second.LocalId);
        Assert.NotEqual(first.LocalId, second.LocalId);
    }

    [Fact]
    public void Address_IsNullWhenDetachedAndChangesAfterReparenting()
    {
        using Document document = new("document");
        DocumentSection firstSection = new("first");
        DocumentSection secondSection = new("second");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");

        Assert.Null(element.ParentSection);
        Assert.Null(element.Address);

        firstSection.Elements.Add(element);

        Assert.Same(firstSection, element.ParentSection);
        Assert.Null(element.Address);

        document.Sections.Add(firstSection);
        document.Sections.Add(secondSection);

        Assert.Equal("s0/e0", element.Address);

        firstSection.Elements.Remove(element);

        Assert.Null(element.ParentSection);
        Assert.Null(element.Address);

        secondSection.Elements.Add(element);

        Assert.Same(secondSection, element.ParentSection);
        Assert.Equal("s1/e0", element.Address);
    }

    [Fact]
    public void GetMarkdownAndToMarkdown_RemainDistinct()
    {
        DocumentSection section = new("stored section markdown");
        section.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "semantic paragraph"));

        Assert.Equal("stored section markdown", section.GetMarkdown());
        Assert.Equal("semantic paragraph", section.ToMarkdown());
    }

    [Fact]
    public void TextAndMarkdown_CanBeNullOrChanged()
    {
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "stored markdown");

        Assert.Null(element.Text);
        Assert.Equal("stored markdown", element.StoredMarkdown);

        element.Text = "plain text";
        element.StoredMarkdown = "updated markdown";

        Assert.Equal("plain text", element.Text);
        Assert.Equal("updated markdown", element.StoredMarkdown);
        Assert.Equal("updated markdown", element.GetMarkdown());
        Assert.Equal("updated markdown", element.ToMarkdown());
    }

    [Fact]
    public void PageReferences_PreserveDuplicateInsertionOrder()
    {
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        DocumentPageReference first = new("page-1");
        DocumentPageReference duplicate = new("page-1");
        DocumentPageReference last = new("page-2");

        element.PageReferences.Add(first);
        element.PageReferences.Add(duplicate);
        element.PageReferences.Add(last);

        Assert.Equal(3, element.PageReferences.Count);
        Assert.Equal(new[] { first, duplicate, last }, element.PageReferences);
        Assert.Equal(new[] { "page-1", "page-1", "page-2" }, element.PageReferences.Select(reference => reference.Value));
    }

    [Fact]
    public void Evidence_PreservesOpaqueValueAndKindAndOrder()
    {
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        DocumentEvidenceReference first = new("evidence-1", "citation");
        DocumentEvidenceReference duplicate = new("evidence-1", "citation");
        DocumentEvidenceReference second = new("evidence-2");

        element.Evidence.Add(first);
        element.Evidence.Add(duplicate);
        element.Evidence.Add(second);

        Assert.Equal(3, element.Evidence.Count);
        Assert.Equal(new[] { first, duplicate, second }, element.Evidence);
        Assert.Equal("evidence-1", element.Evidence[0].Value);
        Assert.Equal("citation", element.Evidence[0].Kind);
        Assert.Equal("evidence-1", element.Evidence[1].Value);
        Assert.Equal("citation", element.Evidence[1].Kind);
        Assert.Equal("evidence-2", element.Evidence[2].Value);
        Assert.Null(element.Evidence[2].Kind);
    }

    [Fact]
    public void Artifacts_PreserveIdentityAndOrder()
    {
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        using MemoryStream firstContent = new(new byte[] { 1 });
        using MemoryStream secondContent = new(new byte[] { 2 });
        using DocumentArtifact first = new("text/plain", firstContent);
        using DocumentArtifact second = new("text/plain", secondContent);

        element.Artifacts.Add(first);
        element.Artifacts.Add(second);
        element.Artifacts.Add(first);

        Assert.Equal(3, element.Artifacts.Count);
        Assert.Same(first, element.Artifacts[0]);
        Assert.Same(second, element.Artifacts[1]);
        Assert.Same(first, element.Artifacts[2]);
        Assert.Equal(new[] { first, second, first }, element.Artifacts);
    }

    [Fact]
    public void ArtifactCollectionMutation_IncrementsAttachedDocumentVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        document.Sections.Add(section);
        section.Elements.Add(element);
        using MemoryStream content = new(new byte[] { 3 });
        using DocumentArtifact artifact = new("text/plain", content);

        long version = document.Version;
        element.Artifacts.Add(artifact);

        Assert.Equal(version + 1, document.Version);
        Assert.Same(artifact, element.Artifacts[0]);
        Assert.Contains(artifact, document.Artifacts);

        element.Artifacts.Remove(artifact);

        Assert.Equal(version + 2, document.Version);
        Assert.Empty(element.Artifacts);
    }

    [Fact]
    public void RawMetadataPageReferencesAndEvidence_AreUnversionedMutableCollections()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        DocumentPageReference page = new("page-raw");
        DocumentEvidenceReference evidence = new("evidence-raw", "opaque-kind");
        element.Metadata["provider-key"] = "provider-value";
        element.PageReferences.Add(page);
        element.Evidence.Add(evidence);

        Assert.True(element.HasMetadata);
        Assert.Equal("provider-value", element.Metadata["provider-key"]);
        Assert.Same(page, element.PageReferences[0]);
        Assert.Same(evidence, element.Evidence[0]);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void ElementCollection_RejectsSelfAndAncestorCycles()
    {
        DocumentSection section = new("section");

        InvalidOperationException selfException = Assert.Throws<InvalidOperationException>(
            () => section.Elements.Add(section));

        DocumentSection child = new("child");
        section.Elements.Add(child);
        InvalidOperationException ancestorException = Assert.Throws<InvalidOperationException>(
            () => child.Elements.Add(section));

        Assert.Contains("cycle", selfException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cycle", ancestorException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(section.Elements);
        Assert.Same(section, child.ParentSection);
        Assert.Empty(child.Elements);
    }

    [Fact]
    public void ElementCollection_RejectsElementsAlreadyAttachedElsewhere()
    {
        DocumentSection firstSection = new("first");
        DocumentSection secondSection = new("second");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        firstSection.Elements.Add(element);

        Assert.Throws<InvalidOperationException>(() => secondSection.Elements.Add(element));

        Assert.Single(firstSection.Elements);
        Assert.Empty(secondSection.Elements);
        Assert.Same(firstSection, element.ParentSection);
        Assert.Same(element, firstSection.Elements[0]);
    }

    [Fact]
    public void ElementCollection_ReplacementRemovalAndClear_DetachElements()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement oldElement = new(DocumentTextKind.Paragraph, "old");
        DocumentTextElement removedElement = new(DocumentTextKind.Paragraph, "removed");
        DocumentTextElement replacement = new(DocumentTextKind.Paragraph, "replacement");
        document.Sections.Add(section);
        section.Elements.Add(oldElement);
        section.Elements.Add(removedElement);

        section.Elements[0] = replacement;

        Assert.Null(oldElement.ParentSection);
        Assert.Null(oldElement.Address);
        Assert.Same(section, replacement.ParentSection);
        Assert.Equal("s0/e0", replacement.Address);

        section.Elements.Remove(replacement);

        Assert.Null(replacement.ParentSection);
        Assert.Null(replacement.Address);

        section.Elements.Clear();

        Assert.Empty(section.Elements);
        Assert.Null(removedElement.ParentSection);
        Assert.Null(removedElement.Address);
    }

    [Fact]
    public void ScalarMutations_IncrementAttachedDocumentVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Header, "stored", "plain");
        document.Sections.Add(section);
        section.Elements.Add(element);

        long version = document.Version;
        element.StoredMarkdown = "updated";
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        element.Text = "updated plain";
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        element.HeaderLevel = 2;
        Assert.Equal(version + 1, document.Version);
    }

    [Fact]
    public void SameScalarAssignments_DoNotChangeAttachedVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Header, "stored", "plain")
        {
            HeaderLevel = 2
        };
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        element.StoredMarkdown = "stored";
        element.Text = "plain";
        element.HeaderLevel = 2;

        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void Constructor_RejectsEmptyMarkdown()
    {
        Assert.Throws<ArgumentException>(
            "markdown",
            () => new DocumentTextElement(DocumentTextKind.Paragraph, ""));
    }
}
