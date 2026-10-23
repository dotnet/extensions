// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction.Tests;

public sealed class DocumentExtractionCleanupTests
{
    [Fact]
    public void Dispose_ThrowingArtifactStillDisposesOtherPendingArtifacts()
    {
        IOException failure = new("synthetic cleanup failure");
        CleanupStream throwing = new(failure);
        CleanupStream later = new();
        using DocumentArtifact first = new("image/png", throwing);
        using DocumentArtifact second = new("image/png", later);
        using DocumentExtractionDocumentBuilder builder = new("cleanup");
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionImage("image", 0, "", artifacts: [first, second])]));

        AggregateException error = Assert.Throws<AggregateException>(() => builder.Dispose());
        Assert.Same(failure, Assert.Single(error.InnerExceptions));
        Assert.Equal(1, throwing.DisposeCount);
        Assert.Equal(1, later.DisposeCount);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        builder.Dispose();
        Assert.Equal(1, throwing.DisposeCount);
        Assert.Equal(1, later.DisposeCount);
    }

    [Fact]
    public void Build_FailurePreservesValidationAndAllCleanupFailures()
    {
        IOException ownedFailure = new("owned cleanup");
        IOException pendingFailure = new("pending cleanup");
        CleanupStream ownedThrowing = new(ownedFailure);
        CleanupStream ownedLater = new();
        CleanupStream pendingThrowing = new(pendingFailure);
        CleanupStream pendingLater = new();
        CleanupStream foreign = new();
        using DocumentArtifact ownedFirst = new("application/json", ownedThrowing);
        using DocumentArtifact ownedSecond = new("application/json", ownedLater);
        using DocumentArtifact pendingFirst = new("image/png", pendingThrowing);
        using DocumentArtifact pendingSecond = new("image/png", pendingLater);
        using DocumentArtifact foreignArtifact = new("image/png", foreign);
        using Document owner = new("other-owner");
        DocumentSection foreignSection = new("");
        DocumentImageElement foreignImage = new("");
        foreignImage.Artifacts.Add(foreignArtifact);
        foreignSection.Elements.Add(foreignImage);
        owner.Sections.Add(foreignSection);
        using DocumentExtractionDocumentBuilder builder = new("failure");
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionText("duplicate", 0, DocumentTextKind.Paragraph, "one")],
            sourceOutputs:
            [
                new DocumentSourceOutput("owned-first", rawContent: ownedFirst),
                new DocumentSourceOutput("owned-second", rawContent: ownedSecond)
            ]));
        builder.AddPage(new DocumentExtractionPage(2,
            [new DocumentExtractionText("duplicate", 0, DocumentTextKind.Paragraph, "two")]));
        builder.AddPage(new DocumentExtractionPage(3,
            [new DocumentExtractionImage("pending", 0, "", artifacts: [pendingFirst, pendingSecond, foreignArtifact])]));

        AggregateException error = Assert.Throws<AggregateException>(() => builder.Build());
        ArgumentException original = Assert.IsType<ArgumentException>(error.InnerExceptions[0]);
        Assert.Contains("duplicate", original.Message);
        Assert.Equal(3, error.Flatten().InnerExceptions.Count);
        Assert.Contains(ownedFailure, error.Flatten().InnerExceptions);
        Assert.Contains(pendingFailure, error.Flatten().InnerExceptions);
        Assert.Equal(1, ownedThrowing.DisposeCount);
        Assert.Equal(1, ownedLater.DisposeCount);
        Assert.Equal(1, pendingThrowing.DisposeCount);
        Assert.Equal(1, pendingLater.DisposeCount);
        Assert.Equal(0, foreign.DisposeCount);
        Assert.True(foreign.CanRead);
        Assert.False(owner.IsDisposed);
        Assert.True(ownedFirst.IsDocumentOwned);
        Assert.False(pendingFirst.IsDocumentOwned);
        builder.Dispose();
        Assert.Equal(1, pendingLater.DisposeCount);
        owner.Dispose();
        Assert.Equal(1, foreign.DisposeCount);
    }

    [Fact]
    public void Build_FailureWithoutCleanupErrorsPreservesOriginalException()
    {
        CleanupStream stream = new();
        using DocumentArtifact artifact = new("image/png", stream);
        using DocumentExtractionDocumentBuilder builder = new("duplicate");
        builder.AddPage(new DocumentExtractionPage(1,
            [new DocumentExtractionImage("image", 0, "", artifacts: [artifact])]));
        builder.AddPage(new DocumentExtractionPage(1, []));

        ArgumentException error = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("Page number 1 is duplicated.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Equal(1, stream.DisposeCount);
    }

    private sealed class CleanupStream(Exception? failure = null) : MemoryStream
    {
        internal int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
            if (disposing && failure is not null)
            {
                throw failure;
            }
        }
    }
}
