// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public partial class DocumentTests
{
    [Fact]
    public void StoresIdentifierAndInitialState()
    {
        using Document document = new("document-id", "stored document");

        Assert.Equal("document-id", document.Identifier);
        Assert.Equal("stored document", document.StoredMarkdown);
        Assert.Empty(document.Sections);
        Assert.Empty(document.Artifacts);
        Assert.False(document.HasMetadata);
        Assert.False(document.IsDisposed);
        Assert.Equal(0, document.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RejectsNullOrEmptyIdentifier(string? identifier)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Document(identifier!));
    }

    [Fact]
    public void GetMarkdown_ReturnsStoredMarkdown()
    {
        using Document document = new("document", "stored markdown");

        Assert.Equal("stored markdown", document.GetMarkdown());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToMarkdown_ReturnsStoredMarkdownOrEmptyWithoutSections(string? markdown)
    {
        using Document document = new("document", markdown);

        Assert.Equal(markdown ?? string.Empty, document.ToMarkdown());
        Assert.Equal(markdown ?? string.Empty, document.GetMarkdown());
    }

    [Fact]
    public void ToMarkdown_ProjectsSectionsInsteadOfStoredMarkdown()
    {
        using Document document = new("document", "stored document markdown");
        DocumentSection section = new("stored section markdown");
        section.Elements.Add(new DocumentImageElement("stored image markdown")
        {
            Source = new Uri("urn:image:semantic"),
            AlternativeText = "semantic image"
        });

        document.Sections.Add(section);

        Assert.Equal("stored document markdown", document.GetMarkdown());
        Assert.Equal("![semantic image](urn:image:semantic)", document.ToMarkdown());
    }

    [Fact]
    public void ToMarkdown_JoinsMultipleSectionProjectionsInDocumentOrder()
    {
        using Document document = new("document", "stored document markdown");
        DocumentSection first = new("stored first");
        DocumentSection second = new("stored second");
        first.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "first semantic"));
        second.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "second semantic"));
        document.Sections.Add(first);
        document.Sections.Add(second);

        Assert.Equal($"first semantic{Environment.NewLine}second semantic", document.ToMarkdown());
    }

    [Fact]
    public void Markdown_SameValueDoesNotIncrementVersion()
    {
        using Document document = new("document", "stored markdown");
        long version = document.Version;

        document.StoredMarkdown = "stored markdown";

        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void Markdown_ChangedValueIncrementsVersion()
    {
        using Document document = new("document", "stored markdown");
        long version = document.Version;

        document.StoredMarkdown = "updated markdown";

        Assert.Equal(version + 1, document.Version);
        Assert.Equal("updated markdown", document.StoredMarkdown);
    }

    [Fact]
    public void SectionCollection_AddsSectionsAndAssignsRootAddresses()
    {
        using Document document = new("document");
        DocumentSection first = new("first");
        DocumentSection second = new("second");

        document.Sections.Add(first);
        document.Sections.Add(second);

        Assert.Equal(2, document.Sections.Count);
        Assert.Equal("s0", first.Address);
        Assert.Equal("s1", second.Address);
        Assert.Null(first.ParentSection);
        Assert.Null(second.ParentSection);
    }

    [Fact]
    public void SectionCollection_RejectsNullSections()
    {
        using Document document = new("document");

        Assert.Throws<ArgumentNullException>("item", () => document.Sections.Add(null!));
        Assert.Empty(document.Sections);
    }

    [Fact]
    public void SectionCollection_RejectsDuplicateSectionOwnership()
    {
        using Document firstDocument = new("first document");
        using Document secondDocument = new("second document");
        DocumentSection section = new("section");

        firstDocument.Sections.Add(section);

        Assert.Throws<InvalidOperationException>(() => firstDocument.Sections.Add(section));
        Assert.Throws<InvalidOperationException>(() => secondDocument.Sections.Add(section));

        Assert.Single(firstDocument.Sections);
        Assert.Empty(secondDocument.Sections);
        Assert.Equal("s0", section.Address);
    }

    [Fact]
    public void AttachedSection_RejectsAncestorCycle()
    {
        using Document document = new("document");
        DocumentSection root = new("root");
        DocumentSection child = new("child");
        root.Elements.Add(child);
        document.Sections.Add(root);

        Assert.Throws<InvalidOperationException>(() => child.Elements.Add(root));

        Assert.Single(root.Elements);
        Assert.Same(child, root.Elements[0]);
        Assert.Empty(child.Elements);
        Assert.Equal("s0/e0", child.Address);
        Assert.Equal("s0", root.Address);
    }

    [Fact]
    public void SectionCollection_InsertSetRemoveAndClear_NotifyVersionAndDetachSections()
    {
        using Document document = new("document");
        DocumentSection first = new("first");
        DocumentSection replacement = new("replacement");

        AssertVersionIncrement(document, () => document.Sections.Add(first));
        AssertVersionIncrement(document, () => document.Sections[0] = replacement);
        Assert.Null(first.Address);
        Assert.Equal("s0", replacement.Address);

        AssertVersionIncrement(document, () => document.Sections.RemoveAt(0));
        Assert.Null(replacement.Address);

        DocumentSection firstToClear = new("first to clear");
        DocumentSection secondToClear = new("second to clear");
        document.Sections.Add(firstToClear);
        document.Sections.Add(secondToClear);
        AssertVersionIncrement(document, () => document.Sections.Clear());
        Assert.Empty(document.Sections);
        Assert.Null(firstToClear.Address);
        Assert.Null(secondToClear.Address);
    }

    [Fact]
    public void ElementCollection_EditsIncrementVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        document.Sections.Add(section);
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement replacement = new(DocumentTextKind.Paragraph, "replacement");

        AssertVersionIncrement(document, () => section.Elements.Add(first));
        AssertVersionIncrement(document, () => section.Elements[0] = replacement);
        Assert.Null(first.Address);
        Assert.Equal("s0/e0", replacement.Address);

        AssertVersionIncrement(document, () => section.Elements.RemoveAt(0));
        Assert.Null(replacement.Address);

        DocumentTextElement firstToClear = new(DocumentTextKind.Paragraph, "first to clear");
        DocumentTextElement secondToClear = new(DocumentTextKind.Paragraph, "second to clear");
        section.Elements.Add(firstToClear);
        section.Elements.Add(secondToClear);
        AssertVersionIncrement(document, () => section.Elements.Clear());
        Assert.Empty(section.Elements);
        Assert.Null(firstToClear.Address);
        Assert.Null(secondToClear.Address);
    }

    [Fact]
    public void EnumerateContent_ReturnsOrdinaryAndNestedElementsInDepthFirstOrder()
    {
        using Document document = new("document");
        DocumentSection first = new("first");
        DocumentTextElement firstText = new(DocumentTextKind.Paragraph, "first text");
        DocumentSection nested = new("nested");
        DocumentTextElement nestedText = new(DocumentTextKind.Paragraph, "nested text");
        DocumentImageElement image = new("image");
        DocumentExtensionElement extension = new("extension-type", "extension");
        DocumentTableElement table = new("table", 1, 1);
        DocumentSection second = new("second");
        DocumentTextElement secondText = new(DocumentTextKind.Paragraph, "second text");

        nested.Elements.Add(nestedText);
        first.Elements.Add(firstText);
        first.Elements.Add(nested);
        first.Elements.Add(image);
        first.Elements.Add(extension);
        first.Elements.Add(table);
        second.Elements.Add(secondText);
        document.Sections.Add(first);
        document.Sections.Add(second);

        DocumentElement[] content = document.EnumerateContent().ToArray();

        Assert.Equal(
            new DocumentElement[] { firstText, nestedText, image, extension, table, secondText },
            content);
    }

    [Fact]
    public void EnumerateContent_DoesNotReturnSectionNodes()
    {
        using Document document = new("document");
        DocumentSection root = new("root");
        DocumentSection nested = new("nested");
        root.Elements.Add(nested);
        nested.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "content"));
        document.Sections.Add(root);

        DocumentElement[] content = document.EnumerateContent().ToArray();

        Assert.NotEmpty(content);
        Assert.All(content, element => Assert.False(element is DocumentSection));
    }

    [Fact]
    public void RootAndChildAddresses_ReflectCurrentHierarchy()
    {
        using Document document = new("document");
        DocumentSection first = new("first");
        DocumentSection nested = new("nested");
        DocumentTextElement firstText = new(DocumentTextKind.Paragraph, "first text");
        DocumentTextElement nestedText = new(DocumentTextKind.Paragraph, "nested text");
        DocumentSection second = new("second");
        DocumentTextElement secondText = new(DocumentTextKind.Paragraph, "second text");

        first.Elements.Add(firstText);
        first.Elements.Add(nested);
        nested.Elements.Add(nestedText);
        second.Elements.Add(secondText);
        document.Sections.Add(first);
        document.Sections.Add(second);

        Assert.Equal("s0", first.Address);
        Assert.Equal("s0/e0", firstText.Address);
        Assert.Equal("s0/e1", nested.Address);
        Assert.Equal("s0/e1/e0", nestedText.Address);
        Assert.Equal("s1", second.Address);
        Assert.Equal("s1/e0", secondText.Address);
    }

    [Fact]
    public void ElementAddress_IsNullWhenDetachedAndChangesAfterReparenting()
    {
        using Document document = new("document");
        DocumentSection first = new("first");
        DocumentSection second = new("second");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");

        Assert.Null(element.Address);
        first.Elements.Add(element);
        document.Sections.Add(first);
        document.Sections.Add(second);
        Assert.Equal("s0/e0", element.Address);

        first.Elements.Remove(element);
        Assert.Null(element.Address);
        second.Elements.Add(element);

        Assert.Equal("s1/e0", element.Address);
    }

    [Fact]
    public void TableCellAddress_UsesTableAndCellCoordinates()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("table", 2, 3);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "cell");
        table.Cells.SetContent(1, 2, content);
        section.Elements.Add(table);
        document.Sections.Add(section);

        Assert.Equal("s0/e0", table.Address);
        Assert.Equal("s0/e0/c1,2", table.Cells[1, 2]!.Address);
        Assert.Equal("s0/e0/c1,2", content.Address);
    }

    [Fact]
    public void LocalId_IsDistinctForDocumentElements()
    {
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement second = new(DocumentTextKind.Paragraph, "second");

        Assert.NotEqual(Guid.Empty, first.LocalId);
        Assert.NotEqual(Guid.Empty, second.LocalId);
        Assert.NotEqual(first.LocalId, second.LocalId);
    }

    [Fact]
    public void Artifacts_RegistersArtifactAddedBeforeElementAttachment()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        using MemoryStream stream = new(new byte[] { 1 });
        using DocumentArtifact artifact = new("application/octet-stream", stream);

        element.Artifacts.Add(artifact);
        section.Elements.Add(element);
        document.Sections.Add(section);

        Assert.Single(document.Artifacts);
        Assert.Same(artifact, document.Artifacts.Single());
        Assert.Same(artifact, element.Artifacts[0]);
    }

    [Fact]
    public void Artifacts_RegistersArtifactAddedAfterElementAttachment()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement element = new(DocumentTextKind.Paragraph, "content");
        section.Elements.Add(element);
        document.Sections.Add(section);
        using MemoryStream stream = new(new byte[] { 2 });
        using DocumentArtifact artifact = new("application/octet-stream", stream);

        element.Artifacts.Add(artifact);

        Assert.Single(document.Artifacts);
        Assert.Same(artifact, document.Artifacts.Single());
    }

    [Fact]
    public void RejectsArtifactAssignedToTwoDifferentDocuments()
    {
        using Document firstDocument = new("first document");
        using Document secondDocument = new("second document");
        DocumentSection firstSection = new("first section");
        DocumentSection secondSection = new("second section");
        DocumentTextElement firstElement = new(DocumentTextKind.Paragraph, "first content");
        DocumentTextElement secondElement = new(DocumentTextKind.Paragraph, "second content");
        using MemoryStream stream = new(new byte[] { 3 });
        using DocumentArtifact artifact = new("application/octet-stream", stream);

        firstSection.Elements.Add(firstElement);
        firstDocument.Sections.Add(firstSection);
        secondSection.Elements.Add(secondElement);
        secondDocument.Sections.Add(secondSection);
        firstElement.Artifacts.Add(artifact);

        Assert.Throws<InvalidOperationException>(() => secondElement.Artifacts.Add(artifact));

        Assert.Single(firstDocument.Artifacts);
        Assert.Empty(secondDocument.Artifacts);
        Assert.Same(artifact, firstElement.Artifacts[0]);
        Assert.Empty(secondElement.Artifacts);

        firstDocument.Dispose();
        secondDocument.Dispose();
    }

    [Fact]
    public void Dispose_DisposesArtifactsRegisteredBeforeAndAfterAttachment()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement beforeElement = new(DocumentTextKind.Paragraph, "before");
        DocumentTextElement afterElement = new(DocumentTextKind.Paragraph, "after");
        using MemoryStream beforeStream = new(new byte[] { 4 });
        using MemoryStream afterStream = new(new byte[] { 5 });
        using DocumentArtifact beforeArtifact = new("application/octet-stream", beforeStream);
        using DocumentArtifact afterArtifact = new("application/octet-stream", afterStream);

        beforeElement.Artifacts.Add(beforeArtifact);
        section.Elements.Add(beforeElement);
        document.Sections.Add(section);
        section.Elements.Add(afterElement);
        afterElement.Artifacts.Add(afterArtifact);

        document.Dispose();

        Assert.True(document.IsDisposed);
        Assert.True(beforeArtifact.IsDisposed);
        Assert.True(afterArtifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => beforeStream.ReadByte());
        Assert.Throws<ObjectDisposedException>(() => afterStream.ReadByte());

        document.Dispose();

        Assert.True(document.IsDisposed);
        Assert.True(beforeArtifact.IsDisposed);
        Assert.True(afterArtifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => beforeStream.ReadByte());
        Assert.Throws<ObjectDisposedException>(() => afterStream.ReadByte());
    }

    [Fact]
    public void Dispose_IsIdempotentAndSetsIsDisposed()
    {
        using Document document = new("document");

        document.Dispose();
        document.Dispose();

        Assert.True(document.IsDisposed);
    }

    [Fact]
    public void Version_IncrementsForSupportedDocumentAndElementHierarchyEdits()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentSection replacement = new("replacement");

        AssertVersionIncrement(document, () => document.StoredMarkdown = "document markdown");
        AssertVersionIncrement(document, () => document.Sections.Add(section));
        AssertVersionIncrement(document, () => document.Sections[0] = replacement);
        AssertVersionIncrement(document, () => document.Sections.RemoveAt(0));

        DocumentSection sectionToClear = new("section to clear");
        document.Sections.Add(sectionToClear);
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement second = new(DocumentTextKind.Paragraph, "second");
        AssertVersionIncrement(document, () => sectionToClear.Elements.Add(first));
        AssertVersionIncrement(document, () => sectionToClear.Elements[0] = second);
        AssertVersionIncrement(document, () => sectionToClear.Elements.RemoveAt(0));
        sectionToClear.Elements.Add(first);
        sectionToClear.Elements.Add(second);
        AssertVersionIncrement(document, () => sectionToClear.Elements.Clear());
    }

    [Fact]
    public void Version_IncrementsForSupportedScalarEdits()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement text = new(DocumentTextKind.Header, "stored text", "plain text");
        DocumentImageElement image = new("stored image")
        {
            Content = new byte[] { 1, 2 },
            MediaType = "image/png",
            Source = new Uri("urn:image:initial"),
            AlternativeText = "initial"
        };

        section.Elements.Add(text);
        section.Elements.Add(image);
        document.Sections.Add(section);

        AssertVersionIncrement(document, () => text.StoredMarkdown = "updated text");
        AssertVersionIncrement(document, () => text.Text = "updated plain text");
        AssertVersionIncrement(document, () => text.HeaderLevel = 2);
        AssertVersionIncrement(document, () => image.StoredMarkdown = "updated image");
        AssertVersionIncrement(document, () => image.Text = "updated image text");
        AssertVersionIncrement(document, () => image.Content = new byte[] { 3, 4 });
        AssertVersionIncrement(document, () => image.MediaType = "image/jpeg");
        AssertVersionIncrement(document, () => image.Source = new Uri("urn:image:updated"));
        AssertVersionIncrement(document, () => image.AlternativeText = "updated");
    }

    [Fact]
    public void Version_IncrementsForSupportedTableAndArtifactEdits()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("table", 1, 1);
        DocumentTextElement firstCellContent = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement replacementCellContent = new(DocumentTextKind.Paragraph, "replacement");
        DocumentTextElement artifactOwner = new(DocumentTextKind.Paragraph, "artifact owner");
        section.Elements.Add(table);
        section.Elements.Add(artifactOwner);
        document.Sections.Add(section);

        AssertVersionIncrement(document, () => table.Cells.SetContent(0, 0, firstCellContent));
        AssertVersionIncrement(document, () => table.Cells.SetContent(0, 0, replacementCellContent));
        AssertVersionIncrement(document, () => table.Cells.SetContent(0, 0, null));

        using MemoryStream firstStream = new(new byte[] { 6 });
        using MemoryStream secondStream = new(new byte[] { 7 });
        using DocumentArtifact firstArtifact = new("application/octet-stream", firstStream);
        using DocumentArtifact secondArtifact = new("application/octet-stream", secondStream);
        AssertVersionIncrement(document, () => artifactOwner.Artifacts.Add(firstArtifact));
        AssertVersionIncrement(document, () => artifactOwner.Artifacts.Remove(firstArtifact));
        artifactOwner.Artifacts.Add(firstArtifact);
        artifactOwner.Artifacts.Add(secondArtifact);
        AssertVersionIncrement(document, () => artifactOwner.Artifacts.Clear());

        document.Dispose();
    }

    [Fact]
    public void Version_DoesNotChangeForSameValueScalarAssignments()
    {
        using Document document = new("document", "stored document");
        DocumentSection section = new("section");
        DocumentTextElement text = new(DocumentTextKind.Header, "stored text", "plain text")
        {
            HeaderLevel = 2
        };
        ReadOnlyMemory<byte> imageContent = new byte[] { 8, 9 };
        Uri imageSource = new("urn:image:same");
        DocumentImageElement image = new("stored image")
        {
            Content = imageContent,
            MediaType = "image/png",
            Source = imageSource,
            AlternativeText = "same"
        };
        section.Elements.Add(text);
        section.Elements.Add(image);
        document.Sections.Add(section);
        long version = document.Version;

        document.StoredMarkdown = "stored document";
        text.StoredMarkdown = "stored text";
        text.Text = "plain text";
        text.HeaderLevel = 2;
        image.StoredMarkdown = "stored image";
        image.Text = null;
        image.Content = imageContent;
        image.MediaType = "image/png";
        image.Source = imageSource;
        image.AlternativeText = "same";

        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void Version_DoesNotChangeWhenTableCellIsAssignedToItself()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("table", 1, 1);
        table.Cells.SetContent(0, 0, new DocumentTextElement(DocumentTextKind.Paragraph, "cell"));
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentTableCell cell = table.Cells[0, 0]!;
        long version = document.Version;

        table.Cells[0, 0] = cell;

        Assert.Same(cell, table.Cells[0, 0]);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void DisposedDocument_RejectsMarkdownAndSectionMutations()
    {
        using Document document = new("document");
        document.Sections.Add(new DocumentSection("section"));
        document.Dispose();

        Assert.Throws<ObjectDisposedException>(() => document.StoredMarkdown = "updated");
        Assert.Throws<ObjectDisposedException>(() => document.Sections.Add(new DocumentSection("added")));
        Assert.Throws<ObjectDisposedException>(() => document.Sections[0] = new DocumentSection("replacement"));
        Assert.Throws<ObjectDisposedException>(() => document.Sections.RemoveAt(0));
        Assert.Throws<ObjectDisposedException>(() => document.Sections.Clear());

        Assert.True(document.IsDisposed);
        Assert.Single(document.Sections);
    }

    [Fact]
    public void DisposedOwner_RejectsAttachedElementScalarMutations()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTextElement text = new(DocumentTextKind.Header, "text", "plain");
        DocumentImageElement image = new("image");
        using MemoryStream existingStream = new(new byte[] { 11 });
        using DocumentArtifact existingArtifact = new("application/octet-stream", existingStream);
        section.Elements.Add(text);
        section.Elements.Add(image);
        text.Artifacts.Add(existingArtifact);
        document.Sections.Add(section);
        document.Dispose();

        Assert.Throws<ObjectDisposedException>(() => text.StoredMarkdown = "updated");
        Assert.Throws<ObjectDisposedException>(() => text.Text = "updated");
        Assert.Throws<ObjectDisposedException>(() => text.HeaderLevel = 2);
        Assert.Throws<ObjectDisposedException>(() => image.StoredMarkdown = "updated");
        Assert.Throws<ObjectDisposedException>(() => image.Text = "updated");
        Assert.Throws<ObjectDisposedException>(() => image.Content = new byte[] { 10 });
        Assert.Throws<ObjectDisposedException>(() => image.MediaType = "image/png");
        Assert.Throws<ObjectDisposedException>(() => image.Source = new Uri("urn:image:disposed"));
        Assert.Throws<ObjectDisposedException>(() => image.AlternativeText = "updated");

        using MemoryStream newStream = new(new byte[] { 12 });
        using DocumentArtifact newArtifact = new("application/octet-stream", newStream);

        Assert.Throws<ObjectDisposedException>(() => text.Artifacts.Add(newArtifact));
        Assert.Throws<ObjectDisposedException>(() => text.Artifacts.Remove(existingArtifact));
        Assert.Throws<ObjectDisposedException>(() => text.Artifacts.Clear());

        Assert.Single(text.Artifacts);
        Assert.Same(existingArtifact, text.Artifacts[0]);
    }

    [Fact]
    public void DisposedOwner_RejectsTableCellMutations()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("table", 1, 1);
        DocumentTextElement original = new(DocumentTextKind.Paragraph, "original");
        table.Cells.SetContent(0, 0, original);
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentTableCell originalCell = table.Cells[0, 0]!;
        document.Dispose();

        DocumentTextElement replacement = new(DocumentTextKind.Paragraph, "replacement");
        Assert.Throws<ObjectDisposedException>(() => table.Cells.SetContent(0, 0, replacement));
        Assert.Throws<ObjectDisposedException>(() => table.Cells[0, 0] = new DocumentTableCell(replacement));
        Assert.Throws<ObjectDisposedException>(() => table.Cells[0, 0] = null);

        Assert.Same(originalCell, table.Cells[0, 0]);
        Assert.Same(original, originalCell.Content);
        Assert.Equal("s0/e0/c0,0", original.Address);
        Assert.Null(replacement.Address);
    }

    [Fact]
    public void AttachedTable_RejectsDanglingRelationshipsWithoutChangingCells()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("table", 1, 2);
        DocumentTextElement content = new(DocumentTextKind.Paragraph, "content");
        table.Cells.SetContent(0, 0, content);
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentSection invalidSection = new("invalid section");

        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 1, invalidSection));
        Assert.Throws<InvalidOperationException>(() => table.Cells.SetContent(0, 1, content));

        Assert.Same(content, table.Cells[0, 0]!.Content);
        Assert.Null(table.Cells[0, 1]);
        Assert.Equal("s0/e0/c0,0", content.Address);
        Assert.Null(invalidSection.Address);
    }

    private static void AssertVersionIncrement(Document document, Action mutation)
    {
        long before = document.Version;

        mutation();

        Assert.Equal(before + 1, document.Version);
    }

    [Fact]
    public void DocumentTraversal_PreservesOriginIdentityAcrossHierarchyChanges()
    {
        using Document document = new("origin");
        DocumentSourceOutput output = new("operation-A");
        DocumentSourcePage page7 = DocumentAnnotationTestData.PdfPage(output);
        DocumentSourcePage page2 = new("urn:frame:page2", 2, sourceOutput: output);
        DocumentTextElement text7 = new(DocumentTextKind.Paragraph, "same text");
        DocumentTextElement text2 = new(DocumentTextKind.Paragraph, "same text");
        DocumentSourceReference region = new(page7, DocumentAnnotationTestData.Polygon());
        text7.Sources.Add(region);
        text2.Sources.Add(new DocumentSourceReference(page2, DocumentAnnotationTestData.Polygon()));
        DocumentSection first = new("");
        DocumentSection second = new("");
        first.Elements.Add(text7);
        second.Elements.Add(text2);
        document.Sections.Add(first);
        document.Sections.Add(second);
        Assert.Equal("s0/e0", text7.Address);
        Assert.Equal(new[] { page7, page2 }, document.SourcePages);

        first.Elements.Remove(text7);
        second.Elements.Insert(0, text7);

        Assert.Equal("s1/e0", text7.Address);
        Assert.Equal("s1/e1", text2.Address);
        Assert.Equal(new[] { text7, text2 }, document.EnumerateContent());
        Assert.Equal(new[] { 7, 2 }, document.SourcePages.Select(page => page.PageNumber));
        Assert.Same(page7, text7.Sources[0].Page);
        Assert.Equal("urn:frame:pdf-A", region.Page.Identifier);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), region.Polygon);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
    }

    [Fact]
    public void BlankPage_PreservesSourceFrameWithoutFabricatedSemanticContent()
    {
        using Document document = new("blank", "");
        DocumentSourceOutput output = new("blank-operation");
        DocumentSourcePage page = new("urn:frame:blank-page2", 2, new DocumentPageDimensions(200, 100),
            DocumentCoordinateUnit.Point, DocumentCoordinateOrigin.BottomLeft, output);
        document.AddSourcePage(page);
        document.Sections.Add(new DocumentSection(""));

        Assert.Equal("", document.GetMarkdown());
        Assert.Equal("", document.ToMarkdown());
        Assert.Empty(document.EnumerateContent());
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Equal(2, page.PageNumber);
        Assert.Equal("urn:frame:blank-page2", page.Identifier);
        Assert.Equal(new DocumentPageDimensions(200, 100), page.Dimensions);
        Assert.Equal(DocumentCoordinateUnit.Point, page.CoordinateUnit);
        Assert.Equal(DocumentCoordinateOrigin.BottomLeft, page.CoordinateOrigin);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(DocumentRawContentStatus.Unavailable, output.ContentStatus);
        Assert.Empty(document.Artifacts);
    }

    [Fact]
    public void DocumentRawOutput_MultiplePagesShareOneBodyWithoutSemanticLeakage()
    {
        using Document document = new("shared-body", "visible");
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentAnnotationTestData.BorrowedNative native = new();
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation-A", native, artifact);
        DocumentSourcePage page7 = DocumentAnnotationTestData.PdfPage(output);
        DocumentSourcePage page2 = new("frame-2", 2, sourceOutput: output);
        document.AddSourcePage(page7);
        document.AddSourcePage(page2);
        document.AddSourcePage(page7);
        document.AddSourceOutput(output);

        Assert.Equal(new[] { page7, page2 }, document.SourcePages);
        Assert.Same(output, page7.SourceOutput);
        Assert.Same(page7.SourceOutput, page2.SourceOutput);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.Same(stream, page2.SourceOutput!.GetRawContent()!.Content);
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());
        Assert.Equal("visible", document.GetMarkdown());
        Assert.Equal("visible", document.ToMarkdown());
        Assert.Empty(document.EnumerateContent());
        Assert.Equal(0, document.Version);
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void DocumentMetadataAndSources_AreConsumableWithoutExtractionResult()
    {
        using Document document = new("shared-annotations");
        DocumentSection section = new("");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible **text**", "visible text");
        section.Elements.Add(text);
        document.Sections.Add(section);
        var payload = new { target = text.LocalId, nested = new { unknown = new object[] { 3, "x" } }, vendorFlag = "kept" };
        DocumentSourceOutput output = new("operation", providerName: "synthetic", modelId: "engine:v1");
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage(output);
        long version = document.Version;
        document.Metadata["vendor-extra"] = payload;
        text.Sources.Add(new DocumentSourceReference(page, DocumentAnnotationTestData.Polygon()));

        Assert.Same(payload, document.Metadata["vendor-extra"]);
        Assert.Equal(text.LocalId, payload.target);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Equal("kept", payload.vendorFlag);
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal("engine:v1", page.SourceOutput!.ModelId);
        Assert.Equal(version, document.Version);
        Assert.Same(text, Assert.Single(document.EnumerateContent()));
        Assert.Equal("visible **text**", document.ToMarkdown());
        Assert.Equal("visible text", text.Text);
    }

    [Fact]
    public void NestedSources_RegisterBeforeAndAfterAttachmentWithoutSemanticVersionChanges()
    {
        using Document document = new("nested");
        DocumentSourceOutput output = new("operation");
        DocumentSourcePage page7 = DocumentAnnotationTestData.PdfPage(output);
        DocumentSourcePage page2 = new("frame-2", 2, sourceOutput: output);
        DocumentSection root = new("");
        DocumentSection nested = new("");
        DocumentTableElement table = new("", 2, 2);
        DocumentTableCell empty = new();
        empty.Sources.Add(new DocumentSourceReference(page7, DocumentAnnotationTestData.Polygon()));
        table.Cells[1, 1] = empty;
        nested.Elements.Add(table);
        root.Elements.Add(nested);
        document.Sections.Add(root);
        long version = document.Version;

        DocumentSourceReference after = new(page2, DocumentAnnotationTestData.Polygon());
        nested.Sources.Add(after);
        empty.Sources.Add(after);
        table.Sources.Add(after);
        document.AddSourcePage(page7);
        document.AddSourceOutput(output);

        Assert.Equal(new[] { page7, page2 }, document.SourcePages);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(version, document.Version);
        Assert.Equal("s0/e0/e0/c1,1", empty.Address);
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        Assert.Same(table, Assert.Single(document.EnumerateContent()));
        nested.Sources[0] = new DocumentSourceReference(page7);
        empty.Sources.RemoveAt(1);
        table.Sources.Clear();
        Assert.Equal(version, document.Version);
        Assert.Equal(new[] { page7, page2 }, document.SourcePages);
        table.Cells.SetContent(0, 0, new DocumentTextElement(DocumentTextKind.Paragraph, "real mutation"));
        Assert.Equal(version + 1, document.Version);
        Assert.Equal($"| real mutation |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
    }

    [Fact]
    public void SourceRegistries_RejectIdentityConflictsAndAllowFrameNamesAcrossOperations()
    {
        using Document document = new("identities");
        DocumentSourceOutput first = new("operation-A");
        DocumentSourceOutput second = new("operation-B");
        DocumentSourcePage firstPage = new("same-frame", 7, sourceOutput: first);
        DocumentSourcePage secondPage = new("same-frame", 7, sourceOutput: second);
        document.AddSourcePage(firstPage);
        document.AddSourcePage(secondPage);
        long version = document.Version;
        Assert.Throws<InvalidOperationException>(() => document.AddSourceOutput(new DocumentSourceOutput("operation-A")));
        Assert.Throws<InvalidOperationException>(() => document.AddSourcePage(new DocumentSourcePage("same-frame", 2, sourceOutput: first)));
        DocumentSourceReference conflicting = new(new DocumentSourcePage("same-frame", 7, sourceOutput: first), DocumentAnnotationTestData.Polygon());
        DocumentSection section = new("");
        document.Sections.Add(section);
        version = document.Version;
        Assert.Throws<InvalidOperationException>(() => section.Sources.Add(conflicting));

        Assert.Empty(section.Sources);
        Assert.Equal(new[] { first, second }, document.SourceOutputs);
        Assert.Equal(new[] { firstPage, secondPage }, document.SourcePages);
        Assert.Equal(version, document.Version);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<DocumentSourcePage>)document.SourcePages).Clear());
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<DocumentSourceOutput>)document.SourceOutputs).Clear());
    }

    [Fact]
    public void RawArtifactOwnership_RejectsSecondDocumentWithoutDisposingBorrowedObjects()
    {
        using Document owner = new("owner");
        using Document other = new("other");
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentAnnotationTestData.BorrowedNative native = new();
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation", native, artifact);
        owner.AddSourceOutput(output);

        Assert.Throws<InvalidOperationException>(() => other.AddSourceOutput(output));
        Assert.Empty(other.SourceOutputs);
        Assert.Empty(other.Artifacts);
        Assert.Same(output, Assert.Single(owner.SourceOutputs));
        Assert.Equal(0, stream.DisposeCount);
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());
        other.Dispose();
        Assert.Equal(0, stream.DisposeCount);
        owner.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void DisposedDocument_RejectsSupplementalRegistrationsAndSourceEdits()
    {
        using Document document = new("disposed");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 1, 1);
        DocumentTableCell cell = new();
        DocumentSourceReference original = new(new("original", 1));
        section.Sources.Add(original);
        cell.Sources.Add(original);
        table.Cells[0, 0] = cell;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;
        document.Dispose();

        Assert.Throws<ObjectDisposedException>(() => document.AddSourceOutput(new DocumentSourceOutput("new")));
        Assert.Throws<ObjectDisposedException>(() => document.AddSourcePage(DocumentAnnotationTestData.PdfPage()));
        Assert.Throws<ObjectDisposedException>(() => section.Sources.Add(new DocumentSourceReference(new("new", 2))));
        Assert.Throws<ObjectDisposedException>(() => section.Sources[0] = new DocumentSourceReference(new("new", 2)));
        Assert.Throws<ObjectDisposedException>(() => cell.Sources.RemoveAt(0));
        Assert.Throws<ObjectDisposedException>(() => cell.Sources.Clear());
        Assert.Same(original, Assert.Single(section.Sources));
        Assert.Same(original, Assert.Single(cell.Sources));
        Assert.Same(original.Page, Assert.Single(document.SourcePages));
        Assert.Empty(document.SourceOutputs);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void AttachedElementAndCell_RejectConflictingSourcesWithoutPartialRegistration()
    {
        using Document document = new("conflicting-annotations");
        DocumentSourceOutput output = new("operation-A");
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage(output);
        DocumentSourceReference original = new(page, DocumentAnnotationTestData.Polygon());
        DocumentSection section = new("");
        DocumentTableElement table = new("", 1, 2);
        DocumentTableCell cell = new();
        cell.Sources.Add(original);
        table.Cells[0, 0] = cell;
        section.Elements.Add(table);
        document.Sections.Add(section);
        section.Sources.Add(original);
        DocumentSourceReference conflict = new(new DocumentSourcePage(page.Identifier, 7, sourceOutput: output), DocumentAnnotationTestData.Polygon());
        long version = document.Version;

        Assert.Throws<InvalidOperationException>(() => section.Sources.Add(conflict));
        Assert.Throws<InvalidOperationException>(() => cell.Sources[0] = conflict);
        DocumentTableCell rejected = new();
        rejected.Sources.Add(conflict);
        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 1] = rejected);
        Assert.Null(rejected.Address);
        Assert.Null(table.Cells[0, 1]);
        Assert.Same(original, Assert.Single(section.Sources));
        Assert.Same(original, Assert.Single(cell.Sources));
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(version, document.Version);
        Assert.Equal("|  |  |", document.ToMarkdown());
        Assert.Throws<ArgumentNullException>(() => section.Sources.Add(null!));
        Assert.Throws<ArgumentNullException>(() => cell.Sources[0] = null!);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void SourcedEmptyCell_RegistersOwnedRawOutputWhenInsertedIntoAttachedTable()
    {
        using Document document = new("late-cell");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 2, 2);
        section.Elements.Add(table);
        document.Sections.Add(section);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentArtifact artifact = new("application/json", stream);
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage(output);
        DocumentTableCell cell = new();
        cell.Sources.Add(new DocumentSourceReference(page, DocumentAnnotationTestData.Polygon()));
        long version = document.Version;
        table.Cells[1, 0] = cell;

        Assert.Equal(version + 1, document.Version);
        Assert.Null(cell.Content);
        Assert.Equal("s0/e0/c1,0", cell.Address);
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());
        Assert.Equal($"|  |  |{Environment.NewLine}|  |  |", document.ToMarkdown());
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void LateSources_EachOwnerImmediatelyRegistersDistinctSourcesWithoutSemanticMutation()
    {
        using Document document = new("late-distinct-sources");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 1, 1);
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "visible **value**", "visible value");
        DocumentTableCell cell = new(text);
        table.Cells[0, 0] = cell;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;

        DocumentAnnotationTestData.CountingStream documentStream = new(new byte[] { 11, 12, 13 });
        DocumentAnnotationTestData.CountingStream elementStream = new(new byte[] { 21, 22, 23 });
        DocumentAnnotationTestData.CountingStream cellStream = new(new byte[] { 31, 32, 33 });
        using DocumentArtifact documentArtifact = new("application/octet-stream", documentStream);
        using DocumentArtifact elementArtifact = new("application/octet-stream", elementStream);
        using DocumentArtifact cellArtifact = new("application/octet-stream", cellStream);
        using DocumentAnnotationTestData.BorrowedNative documentNative = new();
        using DocumentAnnotationTestData.BorrowedNative elementNative = new();
        using DocumentAnnotationTestData.BorrowedNative cellNative = new();
        DocumentSourceOutput[] outputs =
        [
            new("document-operation", documentNative, documentArtifact),
            new("element-operation", elementNative, elementArtifact),
            new("cell-operation", cellNative, cellArtifact)
        ];
        DocumentSourcePage[] pages =
        [
            new("document-frame", 7, sourceOutput: outputs[0]),
            new("element-frame", 2, sourceOutput: outputs[1]),
            new("cell-frame", 9, sourceOutput: outputs[2])
        ];
        DocumentArtifact[] artifacts = [documentArtifact, elementArtifact, cellArtifact];
        DocumentAnnotationTestData.CountingStream[] streams = [documentStream, elementStream, cellStream];
        DocumentAnnotationTestData.BorrowedNative[] natives = [documentNative, elementNative, cellNative];
        byte[][] expectedBytes = [new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 }, new byte[] { 31, 32, 33 }];
        var owners = new[] { section.Sources, text.Sources, cell.Sources };

        Assert.Empty(document.SourcePages);
        Assert.Empty(document.SourceOutputs);
        Assert.Empty(document.Artifacts);
        for (int index = 0; index < owners.Length; index++)
        {
            // No explicit AddSource* calls: each owner must perform its own late registration.
            Assert.DoesNotContain(pages[index], document.SourcePages);
            Assert.DoesNotContain(outputs[index], document.SourceOutputs);
            Assert.DoesNotContain(artifacts[index], document.Artifacts);
            Assert.False(artifacts[index].IsDocumentOwned);
            DocumentSourceReference annotation = new(pages[index], DocumentAnnotationTestData.Polygon());
            owners[index].Add(annotation);

            Assert.Same(annotation, Assert.Single(owners[index]));
            Assert.Equal(index + 1, document.SourcePages.Count);
            Assert.Equal(index + 1, document.SourceOutputs.Count);
            Assert.Equal(index + 1, document.Artifacts.Count);
            for (int registered = 0; registered <= index; registered++)
            {
                Assert.Same(pages[registered], document.SourcePages[registered]);
                Assert.Same(outputs[registered], document.SourceOutputs[registered]);
                Assert.Same(outputs[registered], document.SourcePages[registered].SourceOutput);
                Assert.Contains(artifacts[registered], document.Artifacts);
                Assert.True(artifacts[registered].IsDocumentOwned);
                Assert.Same(artifacts[registered], outputs[registered].GetRawContent());
                Assert.Same(streams[registered], outputs[registered].GetRawContent()!.Content);
                Assert.Same(natives[registered], outputs[registered].GetRawRepresentation());
                Assert.Equal(expectedBytes[registered], streams[registered].ToArray());
                Assert.Equal(0, streams[registered].DisposeCount);
                Assert.Equal(0, natives[registered].DisposeCount);
            }

            Assert.Equal(version, document.Version);
            Assert.Equal("| visible **value** |", document.ToMarkdown());
            Assert.Equal("visible value", text.Text);
            Assert.Equal("s0/e0/c0,0", text.Address);
            Assert.Same(table, Assert.Single(document.EnumerateContent()));
        }

        foreach (var owner in owners)
        {
            owner.Clear();
            Assert.Equal(version, document.Version);
            Assert.Equal(pages, document.SourcePages);
            Assert.Equal(outputs, document.SourceOutputs);
            Assert.Equal("| visible **value** |", document.ToMarkdown());
        }

        document.Dispose();
        document.Dispose();
        for (int index = 0; index < streams.Length; index++)
        {
            Assert.Equal(1, streams[index].DisposeCount);
            Assert.False(streams[index].CanRead);
            Assert.Equal(0, natives[index].DisposeCount);
        }
    }

}
