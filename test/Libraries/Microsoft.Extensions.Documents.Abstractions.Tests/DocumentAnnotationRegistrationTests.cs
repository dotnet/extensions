// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentAnnotationRegistrationTests
{
    [Theory]
    [InlineData("root")]
    [InlineData("element")]
    [InlineData("cell")]
    public void RejectedAttachment_OrdinaryForeignArtifactDoesNotTransferEarlierRegistrations(string route)
    {
        using Document foreign = new("foreign");
        using Document destination = new("destination");
        using Document recovered = new("recovered");
        DocumentSection foreignSection = new("");
        foreign.Sections.Add(foreignSection);
        DocumentAnnotationTestData.CountingStream foreignStream = new(new byte[] { 1 });
        using DocumentArtifact foreignArtifact = new("text/plain", foreignStream);
        foreignSection.Artifacts.Add(foreignArtifact);
        DocumentAnnotationTestData.CountingStream localStream = new(new byte[] { 2 });
        using DocumentArtifact localArtifact = new("text/plain", localStream);
        DocumentAnnotationTestData.CountingStream rawStream = new(new byte[] { 3 });
        using DocumentArtifact rawArtifact = new("application/json", rawStream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("candidate-output", native, rawArtifact);
        DocumentSourcePage page = new("candidate-page", 1, sourceOutput: output);
        DocumentSourceReference annotation = new(page, DocumentAnnotationTestData.Polygon());
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        first.Artifacts.Add(localArtifact);
        first.Sources.Add(annotation);
        DocumentTextElement last = new(DocumentTextKind.Paragraph, "last");
        last.Artifacts.Add(foreignArtifact);
        DocumentTableElement nested = new("", 1, 2);
        nested.Cells.SetContent(0, 0, first);
        nested.Cells.SetContent(0, 1, last);
        DocumentSection candidate = new("");
        candidate.Elements.Add(nested);
        DocumentSection host = new("host");
        destination.Sections.Add(host);
        DocumentTableElement table = new("", 2, 2);
        host.Elements.Add(table);
        DocumentTableCell old = new(new DocumentTextElement(DocumentTextKind.Paragraph, "old"), 2, 2);
        table.Cells[0, 0] = old;
        DocumentTableCell cell = new(nested, 2, 2);
        cell.Sources.Add(annotation);
        if (route == "cell")
        {
            candidate.Elements.Remove(nested);
        }

        long version = destination.Version;
        System.Action attach = route switch
        {
            "root" => () => destination.Sections[0] = candidate,
            "element" => () => host.Elements[0] = candidate,
            _ => () => table.Cells[0, 0] = cell
        };
        Assert.Throws<System.InvalidOperationException>(attach);

        Assert.Equal(version, destination.Version);
        Assert.Same(host, Assert.Single(destination.Sections));
        Assert.Same(table, Assert.Single(host.Elements));
        Assert.Same(old, table.Cells[0, 0]);
        Assert.Same(old, table.Cells.GetCoveringCell(1, 1));
        Assert.Equal("s0/e0/c0,0", old.Content!.Address);
        Assert.Empty(destination.Artifacts);
        Assert.Empty(destination.SourceOutputs);
        Assert.Empty(destination.SourcePages);
        Assert.Null(candidate.Address);
        Assert.Null(candidate.ParentSection);
        Assert.Null(nested.Address);
        Assert.Null(first.Address);
        Assert.Null(last.Address);
        Assert.False(localArtifact.IsDocumentOwned);
        Assert.False(rawArtifact.IsDocumentOwned);
        Assert.True(foreignArtifact.IsDocumentOwned);
        Assert.Same(foreignArtifact, Assert.Single(foreign.Artifacts));

        // Repair the explicit foreign reference, then retry the same candidate in another document.
        last.Artifacts.Clear();
        if (route == "cell")
        {
            DocumentTableElement retryTable = new("", 2, 2);
            retryTable.Cells[0, 0] = cell;
            candidate.Elements.Add(retryTable);
        }

        recovered.Sections.Add(candidate);
        Assert.Equal("s0", candidate.Address);
        Assert.Equal(route == "cell" ? "s0/e0/c0,0/c0,0" : "s0/e0/c0,0", first.Address);
        Assert.Same(output, Assert.Single(recovered.SourceOutputs));
        Assert.Same(page, Assert.Single(recovered.SourcePages));
        Assert.Equal(2, recovered.Artifacts.Count);
        Assert.Contains(localArtifact, recovered.Artifacts);
        Assert.Contains(rawArtifact, recovered.Artifacts);
        destination.Dispose();
        Assert.Equal(0, localStream.DisposeCount);
        Assert.Equal(0, rawStream.DisposeCount);
        Assert.Equal(0, foreignStream.DisposeCount);
        recovered.Dispose();
        Assert.Equal(1, localStream.DisposeCount);
        Assert.Equal(1, rawStream.DisposeCount);
        Assert.Equal(0, foreignStream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
        foreign.Dispose();
        Assert.Equal(1, foreignStream.DisposeCount);
    }

    [Fact]
    public void NestedTableAttachment_RecursivelyRegistersCellContentSourcesAndOwnedRawOutputs()
    {
        using Document document = new("recursive-sources");
        DocumentSection section = new("");
        document.Sections.Add(section);
        long version = document.Version;
        DocumentAnnotationTestData.CountingStream cellStream = new(new byte[] { 41, 42, 43 });
        DocumentAnnotationTestData.CountingStream contentStream = new(new byte[] { 51, 52, 53 });
        using DocumentArtifact cellArtifact = new("application/octet-stream", cellStream);
        using DocumentArtifact contentArtifact = new("application/octet-stream", contentStream);
        using DocumentAnnotationTestData.BorrowedNative cellNative = new();
        using DocumentAnnotationTestData.BorrowedNative contentNative = new();
        DocumentSourceOutput cellOutput = new("nested-cell-operation", cellNative, cellArtifact);
        DocumentSourceOutput contentOutput = new("nested-content-operation", contentNative, contentArtifact);
        DocumentSourcePage cellPage = new("nested-cell-frame", 7, sourceOutput: cellOutput);
        DocumentSourcePage contentPage = new("nested-content-frame", 2, sourceOutput: contentOutput);
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "nested **value**", "nested value");
        text.Sources.Add(new DocumentSourceReference(contentPage, DocumentAnnotationTestData.Polygon()));
        DocumentTableCell innerCell = new(text);
        innerCell.Sources.Add(new DocumentSourceReference(cellPage, DocumentAnnotationTestData.Polygon()));
        DocumentTableElement inner = new("", 1, 1);
        inner.Cells[0, 0] = innerCell;
        DocumentTableElement outer = new("", 1, 1);
        outer.Cells.SetContent(0, 0, inner);

        Assert.Empty(document.SourcePages);
        Assert.Empty(document.SourceOutputs);
        Assert.Empty(document.Artifacts);
        Assert.False(cellArtifact.IsDocumentOwned);
        Assert.False(contentArtifact.IsDocumentOwned);
        Assert.Null(text.Address);
        Assert.Equal(version, document.Version);

        // The only attached node is the outer table; registration must recurse through both cell contents.
        section.Elements.Add(outer);

        Assert.Equal(version + 1, document.Version);
        Assert.Collection(document.SourcePages,
            page => Assert.Same(cellPage, page),
            page => Assert.Same(contentPage, page));
        Assert.Collection(document.SourceOutputs,
            output => Assert.Same(cellOutput, output),
            output => Assert.Same(contentOutput, output));
        Assert.Equal(2, document.Artifacts.Count);
        Assert.Contains(cellArtifact, document.Artifacts);
        Assert.Contains(contentArtifact, document.Artifacts);
        Assert.True(cellArtifact.IsDocumentOwned);
        Assert.True(contentArtifact.IsDocumentOwned);
        Assert.Same(cellArtifact, cellOutput.GetRawContent());
        Assert.Same(contentArtifact, contentOutput.GetRawContent());
        Assert.Same(cellStream, cellOutput.GetRawContent()!.Content);
        Assert.Same(contentStream, contentOutput.GetRawContent()!.Content);
        Assert.Same(cellNative, cellOutput.GetRawRepresentation());
        Assert.Same(contentNative, contentOutput.GetRawRepresentation());
        Assert.Equal(new byte[] { 41, 42, 43 }, cellStream.ToArray());
        Assert.Equal(new byte[] { 51, 52, 53 }, contentStream.ToArray());
        Assert.Equal(0, cellStream.DisposeCount);
        Assert.Equal(0, contentStream.DisposeCount);
        Assert.Equal("s0/e0/c0,0/c0,0", text.Address);
        Assert.Equal("s0/e0/c0,0/c0,0", innerCell.Address);
        Assert.Equal("nested value", text.Text);
        Assert.Equal("| \\| nested **value** \\| |", document.ToMarkdown());
        Assert.Same(outer, Assert.Single(document.EnumerateContent()));

        document.Dispose();
        document.Dispose();
        Assert.Equal(1, cellStream.DisposeCount);
        Assert.Equal(1, contentStream.DisposeCount);
        Assert.False(cellStream.CanRead);
        Assert.False(contentStream.CanRead);
        Assert.Equal(0, cellNative.DisposeCount);
        Assert.Equal(0, contentNative.DisposeCount);
    }

    [Fact]
    public void RejectedSection_ConflictingDescendantOutputsPreflightWithoutOwnershipTransfer()
    {
        using Document destination = new("reject-subtree");
        using Document recovered = new("recovered-subtree");
        using Document secondOwner = new("second-output-owner");
        DocumentAnnotationTestData.CountingStream firstStream = new(new byte[] { 11, 12, 13 });
        DocumentAnnotationTestData.CountingStream secondStream = new(new byte[] { 21, 22, 23 });
        using DocumentArtifact firstArtifact = new("application/json", firstStream);
        using DocumentArtifact secondArtifact = new("application/json", secondStream);
        using DocumentAnnotationTestData.BorrowedNative firstNative = new();
        using DocumentAnnotationTestData.BorrowedNative secondNative = new();
        DocumentSourceOutput firstOutput = new("conflicting-operation", firstNative, firstArtifact);
        DocumentSourceOutput secondOutput = new("conflicting-operation", secondNative, secondArtifact);
        DocumentSourcePage firstFrame = new("first-frame", 7, sourceOutput: firstOutput);
        DocumentSourcePage secondFrame = new("second-frame", 2, sourceOutput: secondOutput);
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "**first**", "first");
        DocumentTextElement second = new(DocumentTextKind.Paragraph, "**second**", "second");
        DocumentSourceReference firstAnnotation = new(firstFrame, DocumentAnnotationTestData.Polygon());
        DocumentSourceReference conflictingAnnotation = new(secondFrame, DocumentAnnotationTestData.Polygon());
        first.Sources.Add(firstAnnotation);
        second.Sources.Add(conflictingAnnotation);
        DocumentSection nested = new("");
        nested.Elements.Add(second);
        DocumentSection candidate = new("");
        candidate.Elements.Add(first);
        candidate.Elements.Add(nested);
        long version = destination.Version;

        Assert.Throws<System.InvalidOperationException>(() => destination.Sections.Add(candidate));

        Assert.Empty(destination.Sections);
        Assert.Equal(version, destination.Version);
        Assert.Empty(destination.SourceOutputs);
        Assert.Empty(destination.SourcePages);
        Assert.Empty(destination.Artifacts);
        Assert.Null(candidate.Address);
        Assert.Null(candidate.ParentSection);
        Assert.Null(first.Address);
        Assert.Null(nested.Address);
        Assert.Null(second.Address);
        Assert.Same(candidate, first.ParentSection);
        Assert.Same(candidate, nested.ParentSection);
        Assert.Same(nested, second.ParentSection);
        Assert.Collection(candidate.Elements, element => Assert.Same(first, element), element => Assert.Same(nested, element));
        Assert.Same(second, Assert.Single(nested.Elements));
        Assert.Same(firstAnnotation, Assert.Single(first.Sources));
        Assert.Same(conflictingAnnotation, Assert.Single(second.Sources));
        Assert.False(firstArtifact.IsDocumentOwned);
        Assert.False(secondArtifact.IsDocumentOwned);
        Assert.Equal(0, firstStream.DisposeCount);
        Assert.Equal(0, secondStream.DisposeCount);
        Assert.True(firstStream.CanRead);
        Assert.True(secondStream.CanRead);
        Assert.Equal(11, firstStream.ReadByte());
        Assert.Equal(21, secondStream.ReadByte());

        // This conflict is intrinsic: remove it explicitly, not merely change the destination.
        Assert.True(second.Sources.Remove(conflictingAnnotation));
        recovered.Sections.Add(candidate);
        Assert.Same(candidate, Assert.Single(recovered.Sections));
        Assert.Same(firstOutput, Assert.Single(recovered.SourceOutputs));
        Assert.Same(firstFrame, Assert.Single(recovered.SourcePages));
        Assert.Same(firstArtifact, Assert.Single(recovered.Artifacts));
        Assert.Equal("s0", candidate.Address);
        Assert.Equal("s0/e0", first.Address);
        Assert.Equal("s0/e1", nested.Address);
        Assert.Equal("s0/e1/e0", second.Address);
        Assert.Equal("**first**" + System.Environment.NewLine + "**second**", recovered.ToMarkdown());
        Assert.Equal(1, recovered.Version);
        Assert.True(firstArtifact.IsDocumentOwned);
        Assert.False(secondArtifact.IsDocumentOwned);
        Assert.Equal(0, firstStream.DisposeCount);
        Assert.Equal(0, secondStream.DisposeCount);

        // The rejected second raw output is still independently transferable to another owner.
        secondOwner.AddSourceOutput(secondOutput);
        Assert.Same(secondOutput, Assert.Single(secondOwner.SourceOutputs));
        Assert.Same(secondArtifact, Assert.Single(secondOwner.Artifacts));
        Assert.True(secondArtifact.IsDocumentOwned);
        destination.Dispose();
        Assert.Equal(0, firstStream.DisposeCount);
        Assert.Equal(0, secondStream.DisposeCount);
        recovered.Dispose();
        recovered.Dispose();
        Assert.Equal(1, firstStream.DisposeCount);
        Assert.False(firstStream.CanRead);
        Assert.Equal(0, secondStream.DisposeCount);
        Assert.True(secondStream.CanRead);
        secondOwner.Dispose();
        secondOwner.Dispose();
        Assert.Equal(1, secondStream.DisposeCount);
        Assert.False(secondStream.CanRead);
        Assert.Equal(0, firstNative.DisposeCount);
        Assert.Equal(0, secondNative.DisposeCount);
    }

    [Fact]
    public void RejectedSection_ConflictingDescendantFramesPreflightWithoutPartialRegistration()
    {
        using Document document = new("reject-frames");
        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 31, 32, 33 });
        using DocumentArtifact artifact = new("application/json", stream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("shared-operation", native, artifact);
        DocumentSourcePage firstFrame = new("same-frame", 7, new DocumentPageDimensions(200, 100), sourceOutput: output);
        DocumentSourcePage secondFrame = new("same-frame", 7, new DocumentPageDimensions(400, 200), sourceOutput: output);
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement second = new(DocumentTextKind.Paragraph, "second");
        first.Sources.Add(new DocumentSourceReference(firstFrame, DocumentAnnotationTestData.Polygon()));
        second.Sources.Add(new DocumentSourceReference(secondFrame, DocumentAnnotationTestData.Polygon()));
        DocumentSection candidate = new("");
        candidate.Elements.Add(first);
        candidate.Elements.Add(second);
        long version = document.Version;

        Assert.Throws<System.InvalidOperationException>(() => document.Sections.Add(candidate));

        Assert.Empty(document.Sections);
        Assert.Empty(document.SourceOutputs);
        Assert.Empty(document.SourcePages);
        Assert.Empty(document.Artifacts);
        Assert.Equal(version, document.Version);
        Assert.Null(candidate.Address);
        Assert.Null(candidate.ParentSection);
        Assert.Null(first.Address);
        Assert.Null(second.Address);
        Assert.Same(candidate, first.ParentSection);
        Assert.Same(candidate, second.ParentSection);
        Assert.Same(firstFrame, Assert.Single(first.Sources).Page);
        Assert.Same(secondFrame, Assert.Single(second.Sources).Page);
        Assert.False(artifact.IsDocumentOwned);
        Assert.False(artifact.IsDisposed);
        document.Dispose();
        Assert.Equal(0, stream.DisposeCount);
        Assert.True(stream.CanRead);
        Assert.Equal(31, stream.ReadByte());
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void RejectedCell_ConflictingCellAndContentSourcesPreserveMergedAnchor()
    {
        using Document document = new("old-anchor-owner");
        using Document recovered = new("replacement-owner");
        DocumentAnnotationTestData.CountingStream oldStream = new(new byte[] { 41, 42, 43 });
        DocumentAnnotationTestData.CountingStream cellStream = new(new byte[] { 51, 52, 53 });
        DocumentAnnotationTestData.CountingStream contentStream = new(new byte[] { 61, 62, 63 });
        using DocumentArtifact oldArtifact = new("application/json", oldStream);
        using DocumentArtifact cellArtifact = new("application/json", cellStream);
        using DocumentArtifact contentArtifact = new("application/json", contentStream);
        DocumentSourceOutput oldOutput = new("old-operation", rawContent: oldArtifact);
        DocumentSourceOutput cellOutput = new("replacement-operation", rawContent: cellArtifact);
        DocumentSourceOutput contentOutput = new("replacement-operation", rawContent: contentArtifact);
        DocumentSourcePage oldFrame = new("old-frame", 7, sourceOutput: oldOutput);
        DocumentSourcePage cellFrame = new("cell-frame", 7, sourceOutput: cellOutput);
        DocumentSourcePage contentFrame = new("content-frame", 2, sourceOutput: contentOutput);
        DocumentTextElement oldText = new(DocumentTextKind.Paragraph, "old **anchor**", "old anchor");
        DocumentTableCell oldCell = new(oldText, 2, 2, "columnHeader");
        oldCell.Sources.Add(new DocumentSourceReference(oldFrame, DocumentAnnotationTestData.Polygon()));
        DocumentTableElement table = new("", 2, 2);
        table.Cells[0, 0] = oldCell;
        DocumentSection section = new("");
        section.Elements.Add(table);
        document.Sections.Add(section);
        DocumentTextElement replacementText = new(DocumentTextKind.Paragraph, "new **anchor**", "new anchor");
        DocumentTableCell replacement = new(replacementText, 2, 2, "rowHeader");
        replacement.Sources.Add(new DocumentSourceReference(cellFrame, DocumentAnnotationTestData.Polygon()));
        DocumentSourceReference conflict = new(contentFrame, DocumentAnnotationTestData.Polygon());
        replacementText.Sources.Add(conflict);
        long version = document.Version;

        Assert.Throws<System.InvalidOperationException>(() => table.Cells[0, 0] = replacement);

        Assert.Same(section, Assert.Single(document.Sections));
        Assert.Same(table, Assert.Single(section.Elements));
        Assert.Same(oldCell, table.Cells[0, 0]);
        Assert.Same(oldText, oldCell.Content);
        Assert.Equal("old anchor", oldText.Text);
        Assert.Equal("old **anchor**", oldText.StoredMarkdown);
        Assert.Equal("s0/e0/c0,0", oldCell.Address);
        Assert.Equal("s0/e0/c0,0", oldText.Address);
        Assert.Equal(2, oldCell.RowSpan);
        Assert.Equal(2, oldCell.ColumnSpan);
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                Assert.Same(oldCell, table.Cells.GetCoveringCell(row, column));
                Assert.Equal(row != 0 || column != 0, table.Cells.IsCovered(row, column));
                if (row != 0 || column != 0)
                {
                    Assert.Null(table.Cells[row, column]);
                }
            }
        }

        Assert.Equal(version, document.Version);
        Assert.Same(oldOutput, Assert.Single(document.SourceOutputs));
        Assert.Same(oldFrame, Assert.Single(document.SourcePages));
        Assert.Same(oldArtifact, Assert.Single(document.Artifacts));
        Assert.True(oldArtifact.IsDocumentOwned);
        Assert.Equal(0, oldStream.DisposeCount);
        Assert.True(oldStream.CanRead);
        Assert.Equal(41, oldStream.ReadByte());
        Assert.Null(replacement.Address);
        Assert.Null(replacementText.Address);
        Assert.Null(replacementText.ParentSection);
        Assert.Same(replacementText, replacement.Content);
        Assert.Same(conflict, Assert.Single(replacementText.Sources));
        Assert.False(cellArtifact.IsDocumentOwned);
        Assert.False(contentArtifact.IsDocumentOwned);
        Assert.Equal(0, cellStream.DisposeCount);
        Assert.Equal(0, contentStream.DisposeCount);
        Assert.True(cellStream.CanRead);
        Assert.True(contentStream.CanRead);
        Assert.Equal(51, cellStream.ReadByte());
        Assert.Equal(61, contentStream.ReadByte());

        // Resolve the intrinsic cell/content source conflict before reusing this exact cell.
        Assert.True(replacementText.Sources.Remove(conflict));
        DocumentTableElement recoveredTable = new("", 2, 2);
        recoveredTable.Cells[0, 0] = replacement;
        DocumentSection recoveredSection = new("");
        recoveredSection.Elements.Add(recoveredTable);
        recovered.Sections.Add(recoveredSection);
        Assert.Same(replacement, recoveredTable.Cells[0, 0]);
        Assert.Equal("s0/e0/c0,0", replacement.Address);
        Assert.Equal("s0/e0/c0,0", replacementText.Address);
        Assert.Equal("new anchor", replacementText.Text);
        Assert.Same(replacement, recoveredTable.Cells.GetCoveringCell(1, 1));
        Assert.Same(cellOutput, Assert.Single(recovered.SourceOutputs));
        Assert.Same(cellFrame, Assert.Single(recovered.SourcePages));
        Assert.Same(cellArtifact, Assert.Single(recovered.Artifacts));
        Assert.True(cellArtifact.IsDocumentOwned);
        Assert.False(contentArtifact.IsDocumentOwned);
        recovered.Dispose();
        recovered.Dispose();
        Assert.Equal(1, cellStream.DisposeCount);
        Assert.False(cellStream.CanRead);
        Assert.Equal(0, contentStream.DisposeCount);
        Assert.True(contentStream.CanRead);
        Assert.Equal(0, oldStream.DisposeCount);
        Assert.True(oldStream.CanRead);
        Assert.Equal(version, document.Version);
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, oldStream.DisposeCount);
        Assert.False(oldStream.CanRead);
        contentArtifact.Dispose();
        contentArtifact.Dispose();
        Assert.Equal(1, contentStream.DisposeCount);
        Assert.False(contentStream.CanRead);
    }

    [Theory]
    [InlineData("output")]
    [InlineData("page")]
    [InlineData("source")]
    [InlineData("subtree")]
    public void ExternallyClosedRawContent_RegistrationRejectsWithoutTakingOwnership(string route)
    {
        using Document document = new("closed-raw");
        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 71, 72, 73 });
        using DocumentArtifact artifact = new("application/json", stream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("closed-operation", native, artifact);
        DocumentSourcePage frame = new("closed-frame", 7, sourceOutput: output);
        DocumentSourceReference annotation = new(frame, DocumentAnnotationTestData.Polygon());
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "preserved");
        text.Sources.Add(annotation);
        DocumentSection candidate = new("");
        candidate.Elements.Add(text);
        DocumentSection host = new("");
        document.Sections.Add(host);
        long version = document.Version;
        stream.Dispose();
        Assert.False(artifact.IsDisposed);
        Assert.False(stream.CanRead);
        Assert.Equal(1, stream.DisposeCount);

        System.Action register = route switch
        {
            "output" => () => document.AddSourceOutput(output),
            "page" => () => document.AddSourcePage(frame),
            "source" => () => host.Sources.Add(annotation),
            "subtree" => () => document.Sections.Add(candidate),
            _ => throw new System.ArgumentOutOfRangeException(nameof(route))
        };
        Assert.Throws<System.InvalidOperationException>(register);

        Assert.Empty(document.SourceOutputs);
        Assert.Empty(document.SourcePages);
        Assert.Empty(document.Artifacts);
        Assert.Empty(host.Sources);
        Assert.Same(host, Assert.Single(document.Sections));
        Assert.Equal(version, document.Version);
        Assert.False(artifact.IsDocumentOwned);
        Assert.False(artifact.IsDisposed);
        Assert.Null(candidate.Address);
        Assert.Null(candidate.ParentSection);
        Assert.Null(text.Address);
        Assert.Same(candidate, text.ParentSection);
        Assert.Same(annotation, Assert.Single(text.Sources));
        Assert.Same(artifact, output.GetRawContent());
        Assert.Same(native, output.GetRawRepresentation());
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.False(stream.CanRead);
        Assert.Equal(0, native.DisposeCount);

        // Count is checked before caller artifact teardown; neither rejection nor document disposal closes it again.
    }
}
