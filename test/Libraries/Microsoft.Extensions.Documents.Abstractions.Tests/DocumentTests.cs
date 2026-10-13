// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTests
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
}
