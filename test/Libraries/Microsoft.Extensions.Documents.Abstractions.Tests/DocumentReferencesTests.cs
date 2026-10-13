// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentReferencesTests
{
    [Fact]
    public void DocumentPageReference_PreservesOpaqueValue()
    {
        const string Value = "provider-page/opaque-007";

        DocumentPageReference reference = new(Value);

        Assert.Equal(Value, reference.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentPageReference_NullOrEmptyValue_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentPageReference(value!));
    }

    [Fact]
    public void DocumentEvidenceReference_PreservesOpaqueValueAndKind()
    {
        const string Value = "provider-evidence:opaque#7";
        const string Kind = "citation-kind";

        DocumentEvidenceReference reference = new(Value, Kind);

        Assert.Equal(Value, reference.Value);
        Assert.Equal(Kind, reference.Kind);
    }

    [Fact]
    public void DocumentEvidenceReference_WithoutKind_HasNullKind()
    {
        DocumentEvidenceReference reference = new("provider-evidence:opaque#8");

        Assert.Null(reference.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentEvidenceReference_NullOrEmptyValue_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentEvidenceReference(value!));
    }

    [Fact]
    public void DocumentArtifact_PreservesMediaTypeContentAndSource()
    {
        using MemoryStream content = new(new byte[] { 1, 2, 3 });
        Uri source = new("urn:document-artifact:opaque");

        using DocumentArtifact artifact = new("application/octet-stream", content, source);

        Assert.Equal("application/octet-stream", artifact.MediaType);
        Assert.Same(content, artifact.Content);
        Assert.Same(source, artifact.Source);
        Assert.False(artifact.IsDisposed);
        Assert.Equal(3, artifact.Content.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DocumentArtifact_NullOrEmptyMediaType_Throws(string? mediaType)
    {
        using MemoryStream content = new();

        Assert.ThrowsAny<ArgumentException>(() => new DocumentArtifact(mediaType!, content));
    }

    [Fact]
    public void DocumentArtifact_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            "content",
            () => new DocumentArtifact("application/octet-stream", null!));
    }

    [Fact]
    public void DocumentArtifact_Dispose_ClosesOwnedStreamAndIsIdempotent()
    {
        using MemoryStream content = new(new byte[] { 1, 2, 3 });
        using DocumentArtifact artifact = new("application/octet-stream", content);

        artifact.Dispose();

        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());

        artifact.Dispose();

        Assert.True(artifact.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => content.ReadByte());
    }
}
