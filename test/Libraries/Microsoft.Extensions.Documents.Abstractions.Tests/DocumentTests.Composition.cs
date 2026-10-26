// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public partial class DocumentTests
{
    [Theory]
    [InlineData("section", false)]
    [InlineData("section", true)]
    [InlineData("text", false)]
    [InlineData("text", true)]
    [InlineData("image", false)]
    [InlineData("image", true)]
    [InlineData("extension", false)]
    [InlineData("extension", true)]
    [InlineData("table", false)]
    [InlineData("table", true)]
    [InlineData("empty-cell", false)]
    [InlineData("empty-cell", true)]
    [InlineData("cell-content", false)]
    [InlineData("cell-content", true)]
    public void Sources_EachOwnerRegistersIndependentlyBeforeAndAfterAttachment(string owner, bool attachFirst)
    {
        using Document document = new("all-owners");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 1, 2);
        DocumentTableCell empty = new();
        DocumentTextElement cellText = new(DocumentTextKind.Paragraph, "cell");
        table.Cells[0, 0] = empty;
        table.Cells.SetContent(0, 1, cellText);
        DocumentElement element = owner switch
        {
            "text" => new DocumentTextElement(DocumentTextKind.Paragraph, "visible"),
            "image" => new DocumentImageElement("visible"),
            "extension" => new DocumentExtensionElement("app", "visible"),
            _ => table
        };
        section.Elements.Add(element);
        var sources = owner switch
        {
            "section" => section.Sources,
            "empty-cell" => empty.Sources,
            "cell-content" => cellText.Sources,
            _ => element.Sources
        };
        if (attachFirst)
        {
            document.Sections.Add(section);
        }

        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 31, 32 });
        using DocumentArtifact artifact = new("application/json", stream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput output = new("operation", native, artifact);
        DocumentSourcePage page = new("frame", 4, sourceOutput: output);
        DocumentSourceReference reference = new(page, rawRepresentation: native);
        long version = document.Version;
        sources.Add(reference);
        Assert.Equal(version, document.Version);
        if (!attachFirst)
        {
            Assert.Empty(document.SourcePages);
            Assert.Empty(document.SourceOutputs);
            Assert.False(artifact.IsDocumentOwned);
            document.Sections.Add(section);
        }

        Assert.Same(reference, Assert.Single(sources));
        Assert.Same(page, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(artifact, Assert.Single(document.Artifacts));
        Assert.True(artifact.IsDocumentOwned);
        Assert.Equal(new byte[] { 31, 32 }, stream.ToArray());
        Assert.Equal(owner is "text" or "image" or "extension" ? "visible" : "|  | cell |", document.ToMarkdown());
        sources.Clear();
        Assert.Same(page, Assert.Single(document.SourcePages));
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.Same(native, reference.GetRawRepresentation());
        Assert.Same(native, output.GetRawRepresentation());
        Assert.Equal(0, native.DisposeCount);
    }

    [Fact]
    public void ApplicationOnlyComposition_ProjectsNestedContentWithoutSources()
    {
        using Document document = new("application", "stored root");
        DocumentSection section = new("stored section");
        DocumentSection nested = new("stored nested");
        DocumentTextElement text = new(DocumentTextKind.Paragraph, "**local**", "local");
        DocumentImageElement image = new("") { Source = new Uri("urn:local:image"), AlternativeText = "local diagram" };
        var payload = new { category = "application", count = 2 };
        DocumentExtensionElement extension = new("app", "extension", payload);
        DocumentTableElement table = new("stored table", 1, 2);
        DocumentTextElement cellText = new(DocumentTextKind.Paragraph, "cell|value", "cell value");
        table.Cells.SetContent(0, 0, cellText);
        table.Cells[0, 1] = new DocumentTableCell();
        DocumentAnnotationTestData.CountingStream stream = new(new byte[] { 7, 8 });
        using DocumentArtifact artifact = new("image/png", stream);
        image.Artifacts.Add(artifact);
        nested.Elements.Add(text);
        nested.Elements.Add(table);
        section.Elements.Add(nested);
        section.Elements.Add(image);
        section.Elements.Add(extension);
        document.Sections.Add(section);

        Assert.Equal($"**local**{Environment.NewLine}| cell\\|value |  |{Environment.NewLine}![local diagram](urn:local:image){Environment.NewLine}extension", document.ToMarkdown());
        Assert.Equal("stored root", document.GetMarkdown());
        Assert.Equal("local", text.Text);
        Assert.Equal("cell value", cellText.Text);
        Assert.Same(payload, extension.Value);
        Assert.Equal(new DocumentElement[] { text, table, image, extension }, document.EnumerateContent());
        Assert.All(new DocumentElement[] { section, nested, text, table, cellText, image, extension }, element => Assert.Empty(element.Sources));
        Assert.All(table.Cells, cell => Assert.Empty(cell!.Sources));
        Assert.Empty(document.SourcePages);
        Assert.Empty(document.SourceOutputs);
        Assert.True(artifact.IsDocumentOwned);
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
    }

    [Fact]
    public void SourceRegistries_NoOperationFramesRejectDuplicateIdsWithoutMutation()
    {
        using Document document = new("unscoped");
        DocumentSourcePage first = new("frame", 1);
        document.AddSourcePage(first);
        document.AddSourcePage(first);
        Assert.Throws<InvalidOperationException>(() => document.AddSourcePage(new("frame", 2)));
        Assert.Throws<ArgumentNullException>(() => document.AddSourcePage(null!));
        Assert.Throws<ArgumentNullException>(() => document.AddSourceOutput(null!));
        Assert.Same(first, Assert.Single(document.SourcePages));
        Assert.Empty(document.SourceOutputs);
        Assert.Empty(document.Artifacts);
        Assert.Equal(0, document.Version);
        Assert.Equal("", document.ToMarkdown());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveSources_IndexAndConflictFailuresAreAtomicForElementsAndCells(bool useCell)
    {
        using Document document = new("collection");
        DocumentSection section = new("");
        DocumentTableElement table = new("", 1, 1);
        DocumentTableCell cell = new();
        table.Cells[0, 0] = cell;
        section.Elements.Add(table);
        document.Sections.Add(section);
        var sources = useCell ? cell.Sources : section.Sources;
        DocumentSourceOutput output = new("operation");
        DocumentSourceReference original = new(new("original", 1, sourceOutput: output));
        sources.Add(original);
        DocumentSourceReference candidate = new(new("candidate", 2, sourceOutput: new("new-operation")));
        DocumentSourceReference conflict = new(new("conflict", 3, sourceOutput: new("operation")));
        long version = document.Version;

        Assert.Throws<ArgumentOutOfRangeException>(() => sources.Insert(-1, candidate));
        Assert.Throws<ArgumentOutOfRangeException>(() => sources.Insert(2, candidate));
        Assert.Throws<ArgumentOutOfRangeException>(() => sources[1] = candidate);
        Assert.Throws<ArgumentOutOfRangeException>(() => sources.RemoveAt(1));
        Assert.Throws<InvalidOperationException>(() => sources[0] = conflict);
        Assert.Throws<InvalidOperationException>(() => sources.Insert(0, conflict));
        Assert.Throws<ArgumentNullException>(() => sources[0] = null!);
        Assert.Same(original, Assert.Single(sources));
        Assert.Same(original.Page, Assert.Single(document.SourcePages));
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Equal(version, document.Version);
        Assert.Equal("|  |", document.ToMarkdown());

        sources.Insert(0, candidate);
        sources[1] = candidate;
        Assert.Equal(new[] { candidate, candidate }, sources);
        Assert.Equal(new[] { original.Page, candidate.Page }, document.SourcePages);
        Assert.Equal(new[] { output, candidate.Page.SourceOutput! }, document.SourceOutputs);
        sources.Clear();
        Assert.Empty(sources);
        Assert.Equal(2, document.SourcePages.Count);
        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void SourceAttachment_ForeignRawBodyRejectsBeforeLocalTransferAndCanRetry()
    {
        using Document foreign = new("foreign");
        using Document destination = new("destination");
        using Document recovered = new("recovered");
        DocumentAnnotationTestData.CountingStream localStream = new(new byte[] { 11 });
        DocumentAnnotationTestData.CountingStream foreignStream = new(new byte[] { 22 });
        using DocumentArtifact localArtifact = new("application/json", localStream);
        using DocumentArtifact foreignArtifact = new("application/json", foreignStream);
        using DocumentAnnotationTestData.BorrowedNative native = new();
        DocumentSourceOutput localOutput = new("local", native, localArtifact);
        DocumentSourceOutput foreignOutput = new("foreign", native, foreignArtifact);
        foreign.AddSourceOutput(foreignOutput);
        DocumentSourceReference localSource = new(new("local-frame", 1, sourceOutput: localOutput), rawRepresentation: native);
        DocumentSourceReference foreignSource = new(new("foreign-frame", 2, sourceOutput: foreignOutput), rawRepresentation: native);
        DocumentSection candidate = new("");
        DocumentTextElement first = new(DocumentTextKind.Paragraph, "first");
        DocumentTextElement last = new(DocumentTextKind.Paragraph, "last");
        first.Sources.Add(localSource);
        last.Sources.Add(foreignSource);
        candidate.Elements.Add(first);
        candidate.Elements.Add(last);

        Assert.Throws<InvalidOperationException>(() => destination.Sections.Add(candidate));
        Assert.Empty(destination.Sections);
        Assert.Empty(destination.SourcePages);
        Assert.Empty(destination.SourceOutputs);
        Assert.Empty(destination.Artifacts);
        Assert.Equal(0, destination.Version);
        Assert.Null(candidate.Address);
        Assert.Null(first.Address);
        Assert.Null(last.Address);
        Assert.Same(candidate, first.ParentSection);
        Assert.Same(candidate, last.ParentSection);
        Assert.False(localArtifact.IsDocumentOwned);
        Assert.True(foreignArtifact.IsDocumentOwned);
        Assert.Same(foreignArtifact, Assert.Single(foreign.Artifacts));
        Assert.Equal(0, localStream.DisposeCount);
        Assert.Equal(0, foreignStream.DisposeCount);
        last.Sources.Clear();
        recovered.Sections.Add(candidate);
        Assert.Same(localSource.Page, Assert.Single(recovered.SourcePages));
        Assert.Same(localOutput, Assert.Single(recovered.SourceOutputs));
        Assert.Same(localArtifact, Assert.Single(recovered.Artifacts));
        Assert.Equal($"first{Environment.NewLine}last", recovered.ToMarkdown());
        destination.Dispose();
        Assert.Equal(0, localStream.DisposeCount);
        recovered.Dispose();
        recovered.Dispose();
        Assert.Equal(1, localStream.DisposeCount);
        Assert.Equal(0, foreignStream.DisposeCount);
        Assert.Same(native, localSource.GetRawRepresentation());
        Assert.Same(native, foreignSource.GetRawRepresentation());
        Assert.Equal(0, native.DisposeCount);
        foreign.Dispose();
        Assert.Equal(1, foreignStream.DisposeCount);
        Assert.Equal(0, native.DisposeCount);
    }
}
