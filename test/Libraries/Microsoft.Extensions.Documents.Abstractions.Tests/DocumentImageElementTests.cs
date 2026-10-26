// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentImageElementTests
{
    [Fact]
    public void PreservesContentMediaTypeSourceAndAlternativeText()
    {
        DocumentImageElement image = new("stored image");
        ReadOnlyMemory<byte> content = new byte[] { 1, 2, 3 };
        Uri source = new("urn:image:opaque-7");

        image.Content = content;
        image.MediaType = "image/png";
        image.Source = source;
        image.AlternativeText = "diagram";

        Assert.True(image.Content.HasValue);
        Assert.Equal(content.ToArray(), image.Content.Value.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Same(source, image.Source);
        Assert.Equal("diagram", image.AlternativeText);
    }

    [Fact]
    public void ToMarkdown_UsesImageLinkWhenSourceIsSet()
    {
        DocumentImageElement image = new("stored image")
        {
            Source = new Uri("urn:image:opaque-8"),
            AlternativeText = "a diagram"
        };

        Assert.Equal("![a diagram](urn:image:opaque-8)", image.ToMarkdown());
        Assert.Equal("stored image", image.GetMarkdown());
    }

    [Fact]
    public void ToMarkdown_UsesStoredMarkdownWithoutSource()
    {
        DocumentImageElement image = new("stored image markdown")
        {
            AlternativeText = "unused alternative text"
        };

        Assert.Null(image.Source);
        Assert.Equal("stored image markdown", image.ToMarkdown());
    }

    [Fact]
    public void SameScalarAssignments_DoNotChangeAttachedVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        ReadOnlyMemory<byte> content = new byte[] { 4, 5 };
        Uri source = new("urn:image:opaque-9");
        DocumentImageElement image = new("stored image")
        {
            Content = content,
            MediaType = "image/jpeg",
            Source = source,
            AlternativeText = "photo"
        };
        document.Sections.Add(section);
        section.Elements.Add(image);
        long version = document.Version;

        image.Content = content;
        image.MediaType = "image/jpeg";
        image.Source = source;
        image.AlternativeText = "photo";

        Assert.Equal(version, document.Version);
    }

    [Fact]
    public void ScalarMutations_IncrementAttachedDocumentVersion()
    {
        using Document document = new("document");
        DocumentSection section = new("section");
        DocumentImageElement image = new("stored image");
        document.Sections.Add(section);
        section.Elements.Add(image);

        ReadOnlyMemory<byte> content = new byte[] { 6, 7 };
        long version = document.Version;
        image.Content = content;
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        image.MediaType = "image/png";
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        image.Source = new Uri("urn:image:opaque-10");
        Assert.Equal(version + 1, document.Version);

        version = document.Version;
        image.AlternativeText = "updated alternative";
        Assert.Equal(version + 1, document.Version);
    }

    [Fact]
    public void ImageOnlyContent_PreservesBytesSourceDescriptionAndOwnedLifetime()
    {
        using Document document = new("image-only", "");
        byte[] bytes = [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02];
        DocumentAnnotationTestData.CountingStream stream = new(bytes);
        Uri source = new("urn:image:page7");
        using DocumentArtifact artifact = new("image/png", stream, source);
        DocumentImageElement image = new("")
        {
            Content = bytes,
            MediaType = "image/png",
            Source = source,
            AlternativeText = "diagram only"
        };
        image.Artifacts.Add(artifact);
        DocumentSourcePage page = DocumentAnnotationTestData.PdfPage();
        image.Sources.Add(new DocumentSourceReference(page, DocumentAnnotationTestData.Polygon()));
        DocumentSection section = new("");
        section.Elements.Add(image);
        document.Sections.Add(section);

        Assert.Equal(bytes, image.Content!.Value.ToArray());
        Assert.Equal(bytes, stream.ToArray());
        Assert.Equal("image/png", image.MediaType);
        Assert.Same(source, image.Source);
        Assert.Same(source, artifact.Source);
        Assert.Equal("diagram only", image.AlternativeText);
        Assert.Null(image.Text);
        Assert.Equal("", image.GetMarkdown());
        Assert.Equal("", document.GetMarkdown());
        Assert.Equal("![diagram only](urn:image:page7)", document.ToMarkdown());
        Assert.Same(image, Assert.Single(document.EnumerateContent()));
        Assert.Same(page, Assert.Single(document.SourcePages));
        document.Dispose();
        document.Dispose();
        Assert.Equal(1, stream.DisposeCount);
        Assert.True(artifact.IsDisposed);
        Assert.Equal(bytes, image.Content.Value.ToArray());
    }

}
