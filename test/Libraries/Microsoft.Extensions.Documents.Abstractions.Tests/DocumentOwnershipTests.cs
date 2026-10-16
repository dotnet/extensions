// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentOwnershipTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Dispose_ThrowingStreamsAttemptsAllArtifactsAndPreservesFailures(int failures)
    {
        using Document document = new("throwing-disposal");
        DocumentSection section = new("");
        document.Sections.Add(section);
        IOException first = new("first cleanup");
        IOException second = new("second cleanup");
        ThrowingDisposeStream firstStream = new(first);
        ThrowingDisposeStream secondStream = new(failures == 2 ? second : null);
        DocumentAnnotationTestData.CountingStream lastStream = new(new byte[] { 9 });
        using DocumentArtifact firstArtifact = new("text/plain", firstStream);
        using DocumentArtifact secondArtifact = new("text/plain", secondStream);
        using DocumentArtifact lastArtifact = new("text/plain", lastStream);
        section.Artifacts.Add(firstArtifact);
        section.Artifacts.Add(secondArtifact);
        section.Artifacts.Add(lastArtifact);

        Exception? error = Record.Exception(document.Dispose);

        Assert.True(document.IsDisposed);
        Assert.True(firstArtifact.IsDisposed);
        Assert.True(secondArtifact.IsDisposed);
        Assert.True(lastArtifact.IsDisposed);
        Assert.Equal(1, firstStream.DisposeCount);
        Assert.Equal(1, secondStream.DisposeCount);
        Assert.Equal(1, lastStream.DisposeCount);
        if (failures == 1)
        {
            Assert.Same(first, error);
        }
        else
        {
            AggregateException aggregate = Assert.IsType<AggregateException>(error);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            Assert.Contains(first, aggregate.InnerExceptions);
            Assert.Contains(second, aggregate.InnerExceptions);
        }

        document.Dispose();
        Assert.Equal(1, firstStream.DisposeCount);
        Assert.Equal(1, secondStream.DisposeCount);
        Assert.Equal(1, lastStream.DisposeCount);
    }

    [Fact]
    public void SectionCollection_SelfAssignmentPreservesVersionAndOwnership()
    {
        using Document document = new("self");
        DocumentSection section = new("root");
        DocumentTextElement child = new(DocumentTextKind.Paragraph, "child");
        section.Elements.Add(child);
        document.Sections.Add(section);
        long version = document.Version;

        document.Sections[0] = section;

        Assert.Same(section, Assert.Single(document.Sections));
        Assert.Equal("s0", section.Address);
        Assert.Equal("s0/e0", child.Address);
        Assert.Same(section, child.ParentSection);
        Assert.Equal(version, document.Version);
        document.Dispose();
        Assert.Throws<ObjectDisposedException>(() => document.Sections[0] = section);
        Assert.Equal(version, document.Version);
    }

    private sealed class ThrowingDisposeStream(Exception? error) : MemoryStream
    {
        internal int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
            if (disposing && error is not null)
            {
                throw error;
            }
        }
    }
}
