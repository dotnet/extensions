// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents an ordered, provider-neutral structured document.
/// </summary>
public sealed class Document : IDisposable
{
    private readonly HashSet<DocumentArtifact> _artifacts = new();
    private readonly Dictionary<string, object?> _metadata = new(StringComparer.Ordinal);
    private string? _markdown;
    private bool _disposed;
    private long _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class.
    /// </summary>
    /// <param name="identifier">The local identifier of the document.</param>
    /// <param name="markdown">The stored markdown representation of the document.</param>
    public Document(string identifier, string? markdown = null)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        _markdown = markdown;
        Sections = new DocumentSectionCollection(this);
    }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets or sets the stored markdown representation of the document.
    /// </summary>
    public string? StoredMarkdown
    {
        get => _markdown;
        set
        {
            ThrowIfDisposed();
            if (!string.Equals(_markdown, value, StringComparison.Ordinal))
            {
                _markdown = value;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets the ordered root sections of the document.
    /// </summary>
    public IList<DocumentSection> Sections { get; }

    /// <summary>
    /// Gets a value indicating whether this document has metadata.
    /// </summary>
    public bool HasMetadata => _metadata.Count > 0;

    /// <summary>
    /// Gets the metadata associated with the document.
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata;

    /// <summary>
    /// Gets the opaque evidence references associated with the document.
    /// </summary>
    public IList<DocumentEvidenceReference> Evidence { get; } = new List<DocumentEvidenceReference>();

    /// <summary>
    /// Gets the version of the in-memory document hierarchy.
    /// </summary>
    /// <remarks>
    /// The version changes when supported document, element, or collection edits occur. It is not a durable revision number.
    /// </remarks>
    public long Version => _version;

    /// <summary>
    /// Gets a value indicating whether this document has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the artifacts associated with this document or its elements.
    /// </summary>
    public IReadOnlyCollection<DocumentArtifact> Artifacts => _artifacts;

    /// <summary>
    /// Enumerates non-section content in depth-first document order.
    /// </summary>
    /// <returns>The content elements in document order.</returns>
    public IEnumerable<DocumentElement> EnumerateContent()
    {
        ThrowIfDisposed();

        foreach (DocumentSection section in Sections)
        {
            foreach (DocumentElement element in EnumerateSection(section))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Gets a markdown projection of the semantic document hierarchy.
    /// </summary>
    /// <returns>The semantic markdown projection.</returns>
    public string ToMarkdown()
    {
        ThrowIfDisposed();
        return Sections.Count == 0
            ? StoredMarkdown ?? string.Empty
            : string.Join(Environment.NewLine, Sections.Select(static section => section.ToMarkdown()));
    }

    /// <summary>
    /// Gets the stored markdown representation of the document.
    /// </summary>
    /// <returns>The stored markdown, or an empty string when none was stored.</returns>
    public string GetMarkdown()
    {
        ThrowIfDisposed();
        return StoredMarkdown ?? string.Empty;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (DocumentArtifact artifact in _artifacts)
        {
            artifact.Dispose();
        }
    }

    internal void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Document));
        }
    }

    internal void NotifyMutation()
    {
        ThrowIfDisposed();
        _version++;
    }

    internal void RegisterArtifacts(DocumentElement element)
    {
        foreach (DocumentArtifact artifact in element.Artifacts)
        {
            RegisterArtifact(artifact);
        }
    }

    internal void RegisterArtifact(DocumentArtifact artifact)
    {
        ThrowIfDisposed();

        if (artifact.OwningDocument is not null && !ReferenceEquals(artifact.OwningDocument, this))
        {
            throw new InvalidOperationException("An artifact cannot belong to more than one document.");
        }

        artifact.SetOwningDocument(this);
        _ = _artifacts.Add(artifact);
    }

    internal string? GetAddress(DocumentElement element)
    {
        if (!ReferenceEquals(element.OwningDocument, this))
        {
            return null;
        }

        return element.Parent switch
        {
            Document => $"s{IndexOfSection((DocumentSection)element)}",
            DocumentSection section => $"{GetAddress(section)}/e{section.Elements.IndexOf(element)}",
            DocumentTableElement table => $"{GetAddress(table)}/c{table.Cells.GetCell(element).RowIndex},{table.Cells.GetCell(element).ColumnIndex}",
            _ => null
        };
    }

    private static IEnumerable<DocumentElement> EnumerateSection(DocumentSection section)
    {
        foreach (DocumentElement element in section.Elements)
        {
            if (element is DocumentSection nestedSection)
            {
                foreach (DocumentElement nestedElement in EnumerateSection(nestedSection))
                {
                    yield return nestedElement;
                }
            }
            else
            {
                yield return element;
            }
        }
    }

    private int IndexOfSection(DocumentSection section)
    {
        int index = Sections.IndexOf(section);
        if (index < 0)
        {
            throw new InvalidOperationException("The section is not owned by this document.");
        }

        return index;
    }
}

internal sealed class DocumentSectionCollection : Collection<DocumentSection>
{
    private readonly Document _owner;

    internal DocumentSectionCollection(Document owner) => _owner = owner;

    protected override void InsertItem(int index, DocumentSection item)
    {
        _ = Throw.IfNull(item);

        _owner.ThrowIfDisposed();
        item.Attach(_owner, _owner);

        try
        {
            base.InsertItem(index, item);
        }
        catch
        {
            item.Detach(_owner);
            throw;
        }

        _owner.NotifyMutation();
    }

    protected override void SetItem(int index, DocumentSection item)
    {
        _ = Throw.IfNull(item);

        _owner.ThrowIfDisposed();
        DocumentSection oldItem = this[index];
        item.Attach(_owner, _owner);

        try
        {
            base.SetItem(index, item);
        }
        catch
        {
            item.Detach(_owner);
            throw;
        }

        oldItem.Detach(_owner);
        _owner.NotifyMutation();
    }

    protected override void RemoveItem(int index)
    {
        _owner.ThrowIfDisposed();
        DocumentSection item = this[index];
        item.Detach(_owner);
        base.RemoveItem(index);
        _owner.NotifyMutation();
    }

    protected override void ClearItems()
    {
        _owner.ThrowIfDisposed();
        foreach (DocumentSection section in this)
        {
            section.Detach(_owner);
        }

        base.ClearItems();
        _owner.NotifyMutation();
    }
}
