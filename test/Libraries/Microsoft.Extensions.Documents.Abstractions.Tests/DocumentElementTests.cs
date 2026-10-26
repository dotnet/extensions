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
    [Theory]
    [InlineData("StoredMarkdown")]
    [InlineData("Text")]
    [InlineData("HeaderLevel")]
    [InlineData("Content")]
    [InlineData("MediaType")]
    [InlineData("Source")]
    [InlineData("AlternativeText")]
    public void DisposedScalarMutation_PreservesBackingValueAndVersion(string property)
    {
        using Document document = new("disposed-scalars");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Header, "original markdown", "original text") { HeaderLevel = 3 };
        Uri source = new("urn:original");
        ReadOnlyMemory<byte> bytes = new byte[] { 7, 8 };
        DocumentImageElement image = new("image")
        {
            Content = bytes,
            MediaType = "image/png",
            Source = source,
            AlternativeText = "original alt"
        };
        section.Elements.Add(text);
        section.Elements.Add(image);
        document.Sections.Add(section);
        long version = document.Version;
        document.Dispose();

        Action edit = property switch
        {
            "StoredMarkdown" => () => text.StoredMarkdown = "rejected",
            "Text" => () => text.Text = "rejected",
            "HeaderLevel" => () => text.HeaderLevel = 5,
            "Content" => () => image.Content = new byte[] { 99 },
            "MediaType" => () => image.MediaType = "image/jpeg",
            "Source" => () => image.Source = new Uri("urn:rejected"),
            _ => () => image.AlternativeText = "rejected"
        };
        Assert.Throws<ObjectDisposedException>(edit);

        Assert.Equal("original markdown", text.StoredMarkdown);
        Assert.Equal("original text", text.Text);
        Assert.Equal(3, text.HeaderLevel);
        Assert.Equal(bytes.ToArray(), image.Content!.Value.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Same(source, image.Source);
        Assert.Equal("original alt", image.AlternativeText);
        Assert.Equal(version, document.Version);
        Assert.Equal("s0/e0", text.Address);
        Assert.Equal("s0/e1", image.Address);
    }

    [Theory]
    [InlineData("element")]
    [InlineData("artifact")]
    public void CollectionSelfAssignment_PreservesVersionAndOwnership(string collection)
    {
        using Document document = new("self");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "text");
        using DocumentArtifact artifact = new("text/plain", new MemoryStream(new byte[] { 12 }));
        text.Artifacts.Add(artifact);
        section.Elements.Add(text);
        document.Sections.Add(section);
        long version = document.Version;
        Action assign = collection == "element" ? () => section.Elements[0] = text : () => text.Artifacts[0] = artifact;

        assign();

        Assert.Equal(version, document.Version);
        Assert.Same(text, Assert.Single(section.Elements));
        Assert.Same(section, text.ParentSection);
        Assert.Equal("s0/e0", text.Address);
        Assert.Same(artifact, Assert.Single(text.Artifacts));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.True(artifact.IsDocumentOwned);
        Assert.False(artifact.IsDisposed);
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(assign);
        Assert.Equal(version, document.Version);
    }

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
    public void Sources_PreserveDuplicateInsertionOrder()
    {
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        DocumentSourcePage page = new("page-1", 1);
        DocumentSourceReference first = new(page);
        DocumentSourceReference duplicate = new(page);
        DocumentSourceReference last = new(new("page-2", 2));

        element.Sources.Add(first);
        element.Sources.Add(duplicate);
        element.Sources.Add(first);
        element.Sources.Add(last);

        Assert.Equal(new[] { first, duplicate, first, last }, element.Sources);
        Assert.Equal(new[] { "page-1", "page-1", "page-1", "page-2" }, element.Sources.Select(reference => reference.Page.Identifier));
        Assert.Equal("content", element.ToMarkdown());
    }

    [Fact]
    public void Sources_InsertReplaceRemoveAndClear_RegisterWithoutSemanticMutation()
    {
        using Document document = new("edits");
        DocumentSection section = new("");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        section.Elements.Add(element);
        document.Sections.Add(section);
        long version = document.Version;
        DocumentSourceReference first = new(new("first", 1, sourceOutput: new("op-1")));
        DocumentSourceReference second = new(new("second", 2, sourceOutput: new("op-2")));
        element.Sources.Insert(0, first);
        element.Sources[0] = second;
        Assert.Same(second, Assert.Single(element.Sources));
        Assert.Equal(new[] { first.Page, second.Page }, document.SourcePages);
        Assert.Equal(new[] { first.Page.SourceOutput!, second.Page.SourceOutput! }, document.SourceOutputs);
        element.Sources.RemoveAt(0);
        element.Sources.Add(first);
        element.Sources.Clear();
        Assert.Empty(element.Sources);
        Assert.Equal(2, document.SourcePages.Count);
        Assert.Equal(version, document.Version);
        Assert.Equal("content", document.ToMarkdown());
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
    public void RawMetadataAndSources_AreUnversionedMutableCollections()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        DocumentSourceReference page = new(new("page-raw", 1));
        element.Metadata["provider-key"] = "provider-value";
        element.Sources.Add(page);

        Assert.True(element.HasMetadata);
        Assert.Equal("provider-value", element.Metadata["provider-key"]);
        Assert.Same(page, element.Sources[0]);
        Assert.Same(page.Page, Assert.Single(document.SourcePages));
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
    public void Constructor_AllowsEmptyMarkdown()
    {
        using Document document = new("empty-constructor");
        DocumentSection section = new("");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "");
        Assert.Equal("", element.StoredMarkdown);
        Assert.Equal("", element.ToMarkdown());
        Assert.Null(element.Text);
        Assert.Null(element.ParentSection);
        Assert.Null(element.Address);
        document.Sections.Add(section);
        section.Elements.Add(element);
        long version = document.Version;

        Assert.Same(section, element.ParentSection);
        Assert.Equal("s0/e0", element.Address);
        Assert.Equal("", document.ToMarkdown());
        element.StoredMarkdown = "";
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void Constructor_RejectsNullMarkdown()
    {
        Assert.Throws<ArgumentNullException>(
            "markdown",
            () => new DocumentTextElement(DocumentTextKind.Paragraph, null!));
    }

    [Fact]
    public void Associations_PreserveAbsentAndPartialGeometryWithoutFabrication()
    {
        using Document document = new("partial");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible", "visible");
        DocumentSourcePage dimensionsOnly = new("dimensions-only", 7, new DocumentPageDimensions(200, 100));
        DocumentSourcePage polygonOnly = new("polygon-only", 2);
        DocumentSourceReference region = new(polygonOnly, DocumentAnnotationTestData.Polygon());
        Assert.Empty(text.Sources);
        text.Sources.Add(region);
        section.Elements.Add(text);
        document.Sections.Add(section);
        document.AddSourcePage(dimensionsOnly);

        Assert.Same(region, Assert.Single(text.Sources));
        Assert.Null(region.GetRawRepresentation());
        Assert.Null(polygonOnly.Dimensions);
        Assert.Null(polygonOnly.CoordinateUnit);
        Assert.Null(polygonOnly.CoordinateOrigin);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), region.Polygon);
        Assert.Equal(new DocumentPageDimensions(200, 100), dimensionsOnly.Dimensions);
        Assert.Null(dimensionsOnly.CoordinateUnit);
        Assert.Null(dimensionsOnly.CoordinateOrigin);
        Assert.Equal(new[] { polygonOnly, dimensionsOnly }, document.SourcePages);
        Assert.Empty(document.SourceOutputs);
        Assert.Equal("visible", document.ToMarkdown());
        Assert.Equal("visible", text.Text);
    }

    [Fact]
    public void EmptyStoredMarkdown_IsSemanticAndNullAssignmentDoesNotMutate()
    {
        using Document document = new("empty");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "");
        section.Elements.Add(text);
        document.Sections.Add(section);
        long version = document.Version;
        Assert.Equal("", document.ToMarkdown());
        Assert.Null(text.Text);

        text.StoredMarkdown = "temporary";
        text.StoredMarkdown = "";
        Assert.Equal(version + 2, document.Version);
        Assert.Equal("", text.GetMarkdown());
        Assert.Equal("", document.ToMarkdown());
        Assert.Throws<ArgumentNullException>("value", () => text.StoredMarkdown = null!);
        Assert.Equal(version + 2, document.Version);
        Assert.Equal("", text.StoredMarkdown);
    }

    [Fact]
    public void Projections_ExcludeRawBodyAndNativeMarkers()
    {
        using Document document = new("projections", "stored document");
        using DocumentAnnotationTestData.BorrowedNative native = new();
        using DocumentArtifact artifact = new("application/json", new MemoryStream(DocumentAnnotationTestData.OriginalBytes()));
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSection section = new("stored section");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible **text**", "visible text");
        DocumentImageElement image = new("") { Source = new Uri("urn:image:page7"), AlternativeText = "diagram only" };
        DocumentExtensionElement extension = new("vendor-extra", "extension");
        DocumentElement[] elements = [section, text, image, extension];
        DocumentSourcePage page = new("page", 1, sourceOutput: output);
        foreach (DocumentElement element in elements)
        {
            element.Sources.Add(new DocumentSourceReference(page, rawRepresentation: native));
        }

        section.Elements.Add(text);
        section.Elements.Add(image);
        section.Elements.Add(extension);
        document.Sections.Add(section);

        Assert.Equal($"visible **text**{Environment.NewLine}![diagram only](urn:image:page7){Environment.NewLine}extension", document.ToMarkdown());
        Assert.Equal("stored document", document.GetMarkdown());
        Assert.Equal("stored section", section.GetMarkdown());
        Assert.Equal("", image.GetMarkdown());
        Assert.Equal("visible text", text.Text);
        Assert.Null(image.Text);
        Assert.Null(extension.Text);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(native, text.Sources[0].GetRawRepresentation());
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), ((MemoryStream)output.GetRawContent()!.Content).ToArray());
        Assert.DoesNotContain(native.Marker, document.ToMarkdown(), StringComparison.Ordinal);
        Assert.DoesNotContain("RAW_BODY_ONLY_91", document.ToMarkdown(), StringComparison.Ordinal);
        Assert.DoesNotContain(typeof(DocumentSourceOutput).GetProperties(), property => property.Name is "RawRepresentation" or "RawContent");
        Assert.DoesNotContain(typeof(DocumentSourceReference).GetProperties(), property => property.Name == "RawRepresentation");
        document.Dispose();
        Assert.Equal(0, native.DisposeCount);
    }
}
