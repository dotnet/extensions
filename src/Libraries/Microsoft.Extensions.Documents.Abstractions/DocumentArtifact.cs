// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents binary content whose lifetime transfers to the document to which it is attached.
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
    internal bool IsDisposed => _disposed;

    /// <summary>
    /// Gets a value indicating whether this artifact's lifetime has been transferred to a document.
    /// </summary>
    public bool IsDocumentOwned => _document is not null;

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
