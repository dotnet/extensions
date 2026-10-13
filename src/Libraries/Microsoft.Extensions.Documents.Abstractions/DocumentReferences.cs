// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents an opaque page reference in provider order.
/// </summary>
public sealed class DocumentPageReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentPageReference"/> class.
    /// </summary>
    /// <param name="value">The opaque page reference value.</param>
    public DocumentPageReference(string value)
    {
        Value = Throw.IfNullOrEmpty(value);
    }

    /// <summary>
    /// Gets the opaque page reference value.
    /// </summary>
    public string Value { get; }
}

/// <summary>
/// Represents an opaque evidence association.
/// </summary>
public sealed class DocumentEvidenceReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentEvidenceReference"/> class.
    /// </summary>
    /// <param name="value">The opaque evidence value.</param>
    /// <param name="kind">An optional opaque evidence kind.</param>
    public DocumentEvidenceReference(string value, string? kind = null)
    {
        Value = Throw.IfNullOrEmpty(value);
        Kind = kind;
    }

    /// <summary>
    /// Gets the opaque evidence value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the optional opaque evidence kind.
    /// </summary>
    public string? Kind { get; }
}

/// <summary>
/// Represents an artifact whose lifetime is owned by the document to which it is associated.
/// </summary>
public sealed class DocumentArtifact : IDisposable
{
    private bool _disposed;
    private Document? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentArtifact"/> class.
    /// </summary>
    /// <param name="mediaType">The media type of the artifact.</param>
    /// <param name="content">The content stream owned by the artifact.</param>
    /// <param name="source">An optional opaque source reference.</param>
    public DocumentArtifact(string mediaType, Stream content, Uri? source = null)
    {
        MediaType = Throw.IfNullOrEmpty(mediaType);
        Content = Throw.IfNull(content);
        Source = source;
    }

    /// <summary>
    /// Gets the media type of the artifact.
    /// </summary>
    public string MediaType { get; }

    /// <summary>
    /// Gets the content stream. The artifact owns this stream.
    /// </summary>
    public Stream Content { get; }

    /// <summary>
    /// Gets the optional opaque source reference.
    /// </summary>
    public Uri? Source { get; }

    /// <summary>
    /// Gets a value indicating whether this artifact has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Content.Dispose();
        }
    }

    internal Document? OwningDocument => _document;

    internal void SetOwningDocument(Document document)
    {
        if (_document is not null && !ReferenceEquals(_document, document))
        {
            throw new InvalidOperationException("An artifact cannot belong to more than one document.");
        }

        _document = document;
    }
}
