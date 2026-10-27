// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentTableCellCollectionTests
{
    [Fact]
    public void EnumeratesNullableCellsInRowMajorOrder()
    {
        DocumentTableElement table = new("stored table", 2, 3);
        DocumentTextElement firstContent = new("first");
        DocumentTextElement lastContent = new("last");

        table.Cells[0, 1] = new DocumentTableCell(firstContent);
        table.Cells[1, 2] = new DocumentTableCell(lastContent);

        DocumentTableCell?[] cells = table.Cells.ToArray();

        Assert.Equal(6, cells.Length);
        Assert.Null(cells[0]);
        Assert.Same(firstContent, cells[1]!.Content);
        Assert.Null(cells[2]);
        Assert.Null(cells[3]);
        Assert.Null(cells[4]);
        Assert.Same(lastContent, cells[5]!.Content);
        Assert.Equal(
            new DocumentTableCell?[] { null, cells[1], null, null, null, cells[5] },
            cells);
    }

    [Fact]
    public void Indexer_AttachesNonSectionElement()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new("cell");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells[0, 0] = new DocumentTableCell(content);

        DocumentTableCell cell = table.Cells[0, 0]!;
        Assert.Same(content, cell.Content);
        Assert.Same(cell, table.Cells[0, 0]);
        Assert.Null(content.ParentSection);
        Assert.Equal("s0/e0/c0,0", content.Address);
    }

    [Fact]
    public void SupportsImageCellContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        ReadOnlyMemory<byte> imageContent = new byte[] { 1, 2, 3 };
        Uri source = new("urn:table-image:opaque-1");
        DocumentImageElement image = new("stored image")
        {
            Content = imageContent,
            MediaType = "image/png",
            Source = source,
            AlternativeText = "diagram"
        };

        table.Cells[0, 0] = new DocumentTableCell(image);

        DocumentImageElement actual = Assert.IsType<DocumentImageElement>(table.Cells[0, 0]!.Content);
        Assert.Same(image, actual);
        Assert.Equal(imageContent.ToArray(), actual.Content!.Value.ToArray());
        Assert.Equal("image/png", actual.MediaType);
        Assert.Same(source, actual.Source);
        Assert.Equal("diagram", actual.AlternativeText);
    }

    [Fact]
    public void ReplacesAndRemovesContent_DetachCellsAndElements()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement oldContent = new("old");
        DocumentTextElement replacementContent = new("replacement");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells[0, 0] = new DocumentTableCell(oldContent);
        DocumentTableCell oldCell = table.Cells[0, 0]!;

        table.Cells[0, 0] = new DocumentTableCell(replacementContent);

        DocumentTableCell replacementCell = table.Cells[0, 0]!;
        Assert.NotSame(oldCell, replacementCell);
        Assert.Same(replacementContent, replacementCell.Content);
        Assert.Null(oldCell.Address);
        Assert.Null(oldContent.Address);
        Assert.Equal("s0/e0/c0,0", replacementCell.Address);
        Assert.Equal("s0/e0/c0,0", replacementContent.Address);

        table.Cells[0, 0] = null;

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(replacementCell.Address);
        Assert.Null(replacementContent.Address);
    }

    [Fact]
    public void RejectsAlreadyOwnedCell()
    {
        DocumentTableElement firstTable = new("first", 1, 1);
        DocumentTableElement secondTable = new("second", 1, 1);
        DocumentTextElement content = new("cell");
        DocumentTableCell cell = new(content);
        firstTable.Cells[0, 0] = cell;

        Assert.Throws<InvalidOperationException>(() => secondTable.Cells[0, 0] = cell);

        Assert.Same(cell, firstTable.Cells[0, 0]);
        Assert.Null(secondTable.Cells[0, 0]);
        Assert.Same(content, firstTable.Cells[0, 0]!.Content);
        Assert.Equal(0, cell.RowIndex);
        Assert.Equal(0, cell.ColumnIndex);
    }

    [Fact]
    public void RejectsSectionContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentSection section = new("section");

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 0] = new DocumentTableCell(section));

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(section.ParentSection);
        Assert.Null(section.Address);
    }

    [Fact]
    public void RejectsTableSelfContent()
    {
        DocumentTableElement table = new("stored table", 1, 1);

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 0] = new DocumentTableCell(table));

        Assert.Null(table.Cells[0, 0]);
        Assert.Null(table.Address);
    }

    [Fact]
    public void RejectsContentThatCreatesRelationshipCycle()
    {
        DocumentTableElement outerTable = new("outer", 1, 1);
        DocumentTableElement innerTable = new("inner", 1, 1);

        outerTable.Cells[0, 0] = new DocumentTableCell(innerTable);

        Assert.Throws<InvalidOperationException>(() => innerTable.Cells[0, 0] = new DocumentTableCell(outerTable));

        Assert.Same(innerTable, outerTable.Cells[0, 0]!.Content);
        Assert.Null(innerTable.Cells[0, 0]);
        Assert.Null(outerTable.Address);
        Assert.Null(innerTable.Address);
    }

    [Fact]
    public void RejectsContentAlreadyAttachedElsewhere()
    {
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new("cell");
        section.Elements.Add(content);

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 0] = new DocumentTableCell(content));

        Assert.Same(section, content.ParentSection);
        Assert.Same(content, section.Elements[0]);
        Assert.Null(table.Cells[0, 0]);
    }

    [Fact]
    public void RejectsDanglingTableRelationships()
    {
        DocumentTableElement table = new("stored table", 1, 2);
        DocumentTextElement firstContent = new("first");
        DocumentTextElement secondContent = new("second");
        DocumentSection invalidReplacement = new("invalid replacement");

        table.Cells[0, 0] = new DocumentTableCell(firstContent);
        table.Cells[0, 1] = new DocumentTableCell(secondContent);

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 0] = new DocumentTableCell(invalidReplacement));
        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 0] = new DocumentTableCell(secondContent));

        Assert.Same(firstContent, table.Cells[0, 0]!.Content);
        Assert.Same(secondContent, table.Cells[0, 1]!.Content);
        Assert.Null(invalidReplacement.ParentSection);
        Assert.Null(invalidReplacement.Address);
        Assert.Null(firstContent.Address);
        Assert.Null(secondContent.Address);
    }

    [Fact]
    public void Mutations_IncrementDocumentVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement firstContent = new("first");
        DocumentTextElement replacementContent = new("replacement");
        document.Sections.Add(section);
        section.Elements.Add(table);

        long version = document.Version;
        table.Cells[0, 0] = new DocumentTableCell(firstContent);
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        table.Cells[0, 0] = new DocumentTableCell(replacementContent);
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        table.Cells[0, 0] = null;
        Assert.Equal(version + 1, document.Version);
    }

    [Fact]
    public void SameContentAssignment_DoesNotIncrementVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentTableElement table = new("stored table", 1, 1);
        DocumentTextElement content = new("content");
        document.Sections.Add(section);
        section.Elements.Add(table);

        table.Cells[0, 0] = new DocumentTableCell(content);
        DocumentTableCell cell = table.Cells[0, 0]!;
        long version = document.Version;

        table.Cells[0, 0] = cell;

        Assert.Equal(version, document.Version);
        Assert.Same(cell, table.Cells[0, 0]);
        Assert.Same(content, cell.Content);
        Assert.Equal("s0/e0/c0,0", content.Address);
        Assert.Equal("| content |", document.ToMarkdown());

        table.Cells[0, 0] = null;
        version = document.Version;
        table.Cells[0, 0] = null;

        Assert.Equal(version, document.Version);
        Assert.Null(table.Cells[0, 0]);
        Assert.Null(cell.Table);
        Assert.Null(content.Address);
        Assert.Equal("|  |", document.ToMarkdown());
    }

    [Fact]
    public void MergedCells_PreserveAnchorSpansRolesAndDistinctCoveredPositions()
    {
        using Document document = new("merged");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        DocumentTableCell anchor = new(new DocumentTextElement("MERGED_A"), 2, 2, "header");
        DocumentTableCell ordinary = new(new DocumentTextElement("B"));
        table.Cells[0, 0] = anchor;
        table.Cells[2, 0] = ordinary;
        section.Elements.Add(table);
        document.Sections.Add(section);

        Assert.Equal(2, anchor.RowSpan);
        Assert.Equal(2, anchor.ColumnSpan);
        Assert.Equal("header", anchor.Role);
        Assert.False(table.Cells.IsCovered(0, 0));
        foreach ((int row, int column) in new[] { (0, 1), (1, 0), (1, 1) })
        {
            Assert.Null(table.Cells[row, column]);
            Assert.True(table.Cells.IsCovered(row, column));
            Assert.Same(anchor, table.Cells.GetCoveringCell(row, column));
        }

        Assert.Null(table.Cells[2, 2]);
        Assert.False(table.Cells.IsCovered(2, 2));
        Assert.Null(table.Cells.GetCoveringCell(2, 2));
        Assert.Equal(new DocumentTableCell?[] { anchor, null, null, null, null, null, ordinary, null, null }, table.Cells.ToArray());
        Assert.Equal(new[] { "s0/e0/c0,0", "s0/e0/c2,0" }, table.Cells.Where(cell => cell is not null).Select(cell => cell!.Address));
        Assert.Equal("s0/e0/c0,0", anchor.Content!.Address);
    }

    [Fact]
    public void MergedCells_OverlapCoveredEditsAndReuseRejectWithoutMutation()
    {
        using Document document = new("invalid-merged");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        DocumentTextElement text = new("MERGED_A");
        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 1, 2, 3 });
        using DocumentArtifact artifact = new("application/octet-stream", stream);
        text.Sources.Add(new(new("merged-page", 1, sourceOutput: new("merged-operation", rawContent: artifact))));
        DocumentTableCell anchor = new(text, 2, 2);
        table.Cells[0, 0] = anchor;
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;
        DocumentTableCell?[] before = table.Cells.ToArray();
        DocumentTableCell overlap = new(2, 1);
        DocumentTableCell candidate = new();

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 1] = candidate);
        Assert.Throws<InvalidOperationException>(() => table.Cells[1, 1] = null);
        Assert.Throws<InvalidOperationException>(() => table.Cells[1, 0] = null);
        table.Cells[2, 2] = candidate;
        long afterCandidate = document.Version;
        Assert.Throws<InvalidOperationException>(() => table.Cells[1, 2] = overlap);
        Assert.Throws<InvalidOperationException>(() => table.Cells[2, 1] = candidate);
        Assert.Equal(afterCandidate, document.Version);
        Assert.Null(overlap.Address);
        Assert.Equal("s0/e0/c2,2", candidate.Address);
        table.Cells[2, 2] = null;
        Assert.Equal(before, table.Cells.ToArray());
        Assert.Equal(version + 2, document.Version);
        Assert.Equal("s0/e0/c0,0", text.Address);
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
        Assert.Equal(0, stream.DisposeCount);

        table.Cells[0, 0] = null;
        Assert.False(table.Cells.IsCovered(1, 1));
        Assert.Null(table.Cells.GetCoveringCell(1, 1));
        Assert.Null(anchor.Address);
        Assert.Null(text.Address);
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    public void MergedCells_OutOfBoundsSpanRejectsWithoutTakingOwnership(int rowSpan, int columnSpan)
    {
        using Document document = new("bounds");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentTableCell candidate = new(rowSpan, columnSpan);
        long version = document.Version;

        Assert.Throws<ArgumentException>("cell", () => table.Cells[2, 2] = candidate);
        Assert.Null(candidate.Address);
        Assert.Null(table.Cells[2, 2]);
        Assert.Equal(version, document.Version);
        table.Cells[0, 0] = candidate;
        Assert.Equal("s0/e0/c0,0", candidate.Address);
        Assert.Equal(version + 1, document.Version);
    }

    [Fact]
    public void MergedCells_ProspectiveSpanCrossingCoveredSlotOnlyRejectsWithoutMutation()
    {
        using Document document = new("covered-slot-overlap");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 3, 3);
        DocumentTextElement anchorText = new("MERGED_A", "merged anchor");
        DocumentTableCell anchor = new(anchorText, 2, 2);
        table.Cells[1, 1] = anchor;
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 61, 62, 63 });
        using DocumentArtifact artifact = new("application/octet-stream", stream);
        DocumentTextElement candidateText = new("candidate **value**", "candidate value");
        candidateText.Sources.Add(new(new("candidate-page", 1, sourceOutput: new("candidate-operation", rawContent: artifact))));
        DocumentTableCell candidate = new(candidateText, 2, 1);
        long version = document.Version;
        DocumentTableCell?[] before = table.Cells.ToArray();
        const string Projection = "<table><tr><td></td><td></td><td></td></tr>" +
            "<tr><td></td><td rowspan=\"2\" colspan=\"2\">MERGED_A</td></tr><tr><td></td></tr></table>";

        // Candidate occupies (0,2) and (1,2), never the existing anchor at (1,1).
        Assert.Null(table.Cells[0, 2]);
        Assert.False(table.Cells.IsCovered(0, 2));
        Assert.Null(table.Cells.GetCoveringCell(0, 2));
        Assert.Null(table.Cells[1, 2]);
        Assert.True(table.Cells.IsCovered(1, 2));
        Assert.Same(anchor, table.Cells.GetCoveringCell(1, 2));
        Assert.Equal(Projection, document.ToMarkdown());

        Assert.Throws<InvalidOperationException>(() => table.Cells[0, 2] = candidate);

        Assert.Equal(before, table.Cells.ToArray());
        Assert.Equal(version, document.Version);
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                bool inAnchor = row >= 1 && column >= 1;
                Assert.Equal(inAnchor && (row != 1 || column != 1), table.Cells.IsCovered(row, column));
                Assert.Same(inAnchor ? anchor : null, table.Cells.GetCoveringCell(row, column));
            }
        }

        Assert.Same(anchor, table.Cells[1, 1]);
        Assert.Equal("s0/e0/c1,1", anchor.Address);
        Assert.Equal("s0/e0/c1,1", anchorText.Address);
        Assert.Null(candidate.Address);
        Assert.Null(candidateText.Address);
        Assert.False(artifact.IsDocumentOwned);
        Assert.Empty(document.Artifacts);
        Assert.Empty(document.SourcePages);
        Assert.Empty(document.SourceOutputs);
        Assert.Equal(new byte[] { 61, 62, 63 }, stream.ToArray());
        Assert.Equal(0, stream.DisposeCount);
        Assert.Equal(Projection, document.ToMarkdown());

        // Success in another attached table proves rejection took neither cell nor content ownership.
        using Document other = new("candidate-owner");
        DocumentSection otherSection = new("");
        DocumentTableElement otherTable = new("", 2, 1);
        otherSection.Elements.Add(otherTable);
        other.Sections.Add(otherSection);
        long otherVersion = other.Version;
        otherTable.Cells[0, 0] = candidate;
        Assert.Same(candidate, otherTable.Cells[0, 0]);
        Assert.Equal("s0/e0/c0,0", candidate.Address);
        Assert.Equal("s0/e0/c0,0", candidateText.Address);
        Assert.Same(candidate, otherTable.Cells.GetCoveringCell(1, 0));
        Assert.True(otherTable.Cells.IsCovered(1, 0));
        Assert.Equal(otherVersion + 1, other.Version);
        Assert.True(artifact.IsDocumentOwned);
        Assert.Same(artifact, Assert.Single(other.Artifacts));
        Assert.Same(artifact, Assert.Single(other.SourceOutputs).GetRawContent());
        Assert.Same(candidateText.Sources[0].Page, Assert.Single(other.SourcePages));
        Assert.Equal("<table><tr><td rowspan=\"2\" colspan=\"1\">candidate **value**</td></tr><tr></tr></table>", other.ToMarkdown());
        Assert.Equal("candidate value", candidateText.Text);
        Assert.Equal(version, document.Version);
        Assert.Equal(before, table.Cells.ToArray());

        document.Dispose();
        Assert.Equal(0, stream.DisposeCount);
        Assert.Equal(new byte[] { 61, 62, 63 }, stream.ToArray());
        other.Dispose();
        other.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public void MergedAnchorReplacement_ReleasesCoveredSlotsAndRetainsRegisteredRawOwnership()
    {
        using Document document = new("replacement-lifetime");
        DocumentAnnotationTestData.CountingStream stream = new(DocumentAnnotationTestData.OriginalBytes());
        using DocumentArtifact artifact = new("application/json", stream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("operation-A", native, artifact);
        DocumentSourcePage frame = DocumentAnnotationTestData.PdfPage(output);
        DocumentTextElement oldText = new("lO0? &", "lO0? &");
        DocumentTableCell oldAnchor = new(oldText, 2, 2, "rowHeader");
        oldAnchor.Sources.Add(new DocumentSourceReference(frame, DocumentAnnotationTestData.Polygon()));
        DocumentTableElement table = new("stored table", 4, 4);
        table.Cells[0, 0] = oldAnchor;
        DocumentSection section = new("");
        section.Elements.Add(table);
        document.Sections.Add(section);
        long version = document.Version;
        Assert.Same(oldAnchor, table.Cells.GetCoveringCell(1, 1));
        Assert.True(table.Cells.IsCovered(1, 1));
        Assert.True(artifact.IsDocumentOwned);

        DocumentTextElement replacementText = new("replacement?", "replacement?");
        DocumentTableCell replacement = new(replacementText);
        table.Cells[0, 0] = replacement;

        Assert.Equal(version + 1, document.Version);
        Assert.Same(replacement, table.Cells.GetCoveringCell(0, 0));
        Assert.Null(oldAnchor.Address);
        Assert.Null(oldText.Address);
        Assert.Equal("s0/e0/c0,0", replacement.Address);
        Assert.Equal("s0/e0/c0,0", replacementText.Address);
        foreach ((int row, int column) in new[] { (0, 1), (1, 0), (1, 1) })
        {
            Assert.Null(table.Cells[row, column]);
            Assert.Null(table.Cells.GetCoveringCell(row, column));
            Assert.False(table.Cells.IsCovered(row, column));
        }

        Assert.Equal(
            $"| replacement? |  |  |  |{Environment.NewLine}|  |  |  |  |{Environment.NewLine}" +
            $"|  |  |  |  |{Environment.NewLine}|  |  |  |  |",
            document.ToMarkdown());
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.True(artifact.IsDocumentOwned);
        Assert.Equal(0, stream.DisposeCount);
        Assert.True(stream.CanRead);
        Assert.Equal(123, output.GetRawContent()!.Content.ReadByte());
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());

        DocumentTableCell empty = new();
        table.Cells[0, 1] = empty;
        Assert.Equal(version + 2, document.Version);
        Assert.Equal("s0/e0/c0,1", empty.Address);
        Assert.Same(empty, table.Cells.GetCoveringCell(0, 1));
        Assert.False(table.Cells.IsCovered(0, 1));
        table.Cells[2, 2] = oldAnchor;

        Assert.Equal(version + 3, document.Version);
        Assert.Equal(4, table.Cells.RowCount);
        Assert.Equal(4, table.Cells.ColumnCount);
        Assert.Equal(2, oldAnchor.RowIndex);
        Assert.Equal(2, oldAnchor.ColumnIndex);
        Assert.Equal(2, oldAnchor.RowSpan);
        Assert.Equal(2, oldAnchor.ColumnSpan);
        Assert.Equal("rowHeader", oldAnchor.Role);
        Assert.Equal("s0/e0/c2,2", oldAnchor.Address);
        Assert.Equal("s0/e0/c2,2", oldText.Address);
        Assert.Equal("lO0? &", oldText.Text);
        Assert.Equal("replacement?", replacementText.Text);
        Assert.Null(empty.Content);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                DocumentTableCell? covering = null;
                if (row >= 2 && column >= 2)
                {
                    covering = oldAnchor;
                }
                else if (row == 0 && column == 0)
                {
                    covering = replacement;
                }
                else if (row == 0 && column == 1)
                {
                    covering = empty;
                }

                Assert.Same(covering, table.Cells.GetCoveringCell(row, column));
                Assert.Equal(row >= 2 && column >= 2 && (row != 2 || column != 2), table.Cells.IsCovered(row, column));
            }
        }

        Assert.Equal(
            new DocumentTableCell?[]
            {
                replacement, empty, null, null, null, null, null, null,
                null, null, oldAnchor, null, null, null, null, null
            },
            table.Cells.ToArray());
        Assert.Equal(
            "<table><tr><td rowspan=\"1\" colspan=\"1\">replacement?</td><td rowspan=\"1\" colspan=\"1\"></td><td></td><td></td></tr>" +
            "<tr><td></td><td></td><td></td><td></td></tr>" +
            "<tr><td></td><td></td><th rowspan=\"2\" colspan=\"2\" data-kind=\"rowHeader\">lO0? &amp;</th></tr>" +
            "<tr><td></td><td></td></tr></table>",
            document.ToMarkdown());
        Assert.Equal("stored table", table.Markdown);
        Assert.Same(frame, Assert.Single(document.SourcePages));
        Assert.Same(frame, oldAnchor.Sources[0].Page);
        Assert.Equal(DocumentAnnotationTestData.Polygon(), oldAnchor.Sources[0].Polygon);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.Same(artifact, output.GetRawContent());
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal(0, stream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        Assert.True(stream.CanRead);
        Assert.Equal(34, output.GetRawContent()!.Content.ReadByte());
        Assert.Equal(DocumentAnnotationTestData.OriginalBytes(), stream.ToArray());
        Assert.Equal(version + 3, document.Version);

        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.True(artifact.IsDisposed);
        Assert.Equal(0, native.DisposeCount);
    }
}
