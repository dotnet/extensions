// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents an ordered, provider-neutral structured document.
/// </summary>
public sealed class Document : IDisposable
{
    private readonly HashSet<DocumentArtifact> _artifacts = new();
    private readonly Dictionary<string, object?> _metadata = new(StringComparer.Ordinal);
    private readonly List<DocumentSourceOutput> _sourceOutputs = [];
    private readonly List<DocumentSourcePage> _sourcePages = [];
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
        SourceOutputs = _sourceOutputs.AsReadOnly();
        SourcePages = _sourcePages.AsReadOnly();
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
    internal bool HasMetadata => _metadata.Count > 0;

    /// <summary>
    /// Gets the metadata associated with the document.
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata;

    /// <summary>Gets registered source operations, with explicit break-glass access.</summary>
    public IReadOnlyList<DocumentSourceOutput> SourceOutputs { get; }

    /// <summary>Gets registered source frames, independently of assembled section order.</summary>
    public IReadOnlyList<DocumentSourcePage> SourcePages { get; }

    /// <summary>Registers a source operation and transfers ownership of its original-content artifact.</summary>
    public void AddSourceOutput(DocumentSourceOutput sourceOutput)
    {
        ThrowIfDisposed();
        ValidateSourceOutput(Throw.IfNull(sourceOutput), _sourceOutputs);
        if (!_sourceOutputs.Contains(sourceOutput))
        {
            if (sourceOutput.GetRawContent() is { } content)
            {
                RegisterArtifact(content);
            }

            _sourceOutputs.Add(sourceOutput);
        }
    }

    /// <summary>Registers a source frame and its source operation, when supplied.</summary>
    public void AddSourcePage(DocumentSourcePage page)
    {
        ThrowIfDisposed();
        ValidateSourcePage(Throw.IfNull(page), _sourcePages);
        if (page.SourceOutput is { } output)
        {
            ValidateSourceOutput(output, _sourceOutputs);
            AddSourceOutput(output);
        }

        if (!_sourcePages.Contains(page))
        {
            _sourcePages.Add(page);
        }
    }

    /// <summary>
    /// Gets the version of the in-memory document hierarchy.
    /// </summary>
    /// <remarks>
    /// The version changes when supported document, element, or collection edits occur. It is not a durable revision number.
    /// </remarks>
    internal long Version => _version;

    /// <summary>
    /// Gets a value indicating whether this document has been disposed.
    /// </summary>
    internal bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the artifacts associated with this document or its elements.
    /// </summary>
    internal IReadOnlyCollection<DocumentArtifact> Artifacts => _artifacts;

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
    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Report stream cleanup failures only after attempting every owned artifact.")]
    [SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods", Justification = "Report stream cleanup failures only after attempting every owned artifact.")]
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        List<Exception>? failures = null;
        foreach (DocumentArtifact artifact in _artifacts)
        {
            try
            {
                artifact.Dispose();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        if (failures is { Count: 1 })
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        else if (failures is { Count: > 1 })
        {
            throw new AggregateException(failures);
        }
    }

    internal static IEnumerable<DocumentSourceReference> EnumerateSources(DocumentElement element)
    {
        foreach (DocumentSourceReference source in element.Sources)
        {
            yield return source;
        }

        if (element is DocumentSection section)
        {
            foreach (DocumentElement child in section.Elements)
            {
                foreach (DocumentSourceReference source in EnumerateSources(child))
                {
                    yield return source;
                }
            }
        }
        else if (element is DocumentTableElement table)
        {
            foreach (DocumentTableCell? cell in table.Cells)
            {
                if (cell is null)
                {
                    continue;
                }

                foreach (DocumentSourceReference source in cell.Sources)
                {
                    yield return source;
                }

                if (cell.Content is { } content)
                {
                    foreach (DocumentSourceReference source in EnumerateSources(content))
                    {
                        yield return source;
                    }
                }
            }
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
        foreach (DocumentSourceReference source in element.Sources)
        {
            RegisterSource(source);
        }

        if (element is DocumentTableElement table)
        {
            foreach (DocumentTableCell? cell in table.Cells)
            {
                if (cell is not null)
                {
                    foreach (DocumentSourceReference source in cell.Sources)
                    {
                        RegisterSource(source);
                    }
                }
            }
        }

        foreach (DocumentArtifact artifact in element.Artifacts)
        {
            RegisterArtifact(artifact);
        }
    }

    internal void RegisterSource(DocumentSourceReference source) => AddSourcePage(source.Page);

    internal void ValidateSources(IEnumerable<DocumentSourceReference> sources)
    {
        List<DocumentSourceOutput> outputs = new(_sourceOutputs);
        List<DocumentSourcePage> pages = new(_sourcePages);
        foreach (DocumentSourceReference source in sources)
        {
            ValidateSourcePage(source.Page, pages);
            pages.Add(source.Page);
            if (source.Page.SourceOutput is { } output)
            {
                ValidateSourceOutput(output, outputs);
                outputs.Add(output);
            }
        }
    }

    private static void ValidateSourcePage(DocumentSourcePage page, IEnumerable<DocumentSourcePage> pages)
    {
        if (pages.Any(item => item.Identifier == page.Identifier &&
            item.SourceOutput?.Identifier == page.SourceOutput?.Identifier && !ReferenceEquals(item, page)))
        {
            throw new InvalidOperationException("A source frame identifier must identify one frame instance within its operation.");
        }
    }

    private void ValidateSourceOutput(DocumentSourceOutput source, IEnumerable<DocumentSourceOutput> outputs)
    {
        if (outputs.Any(item => item.Identifier == source.Identifier && !ReferenceEquals(item, source)))
        {
            throw new InvalidOperationException("A source operation identifier must identify one output instance.");
        }

        if (source.GetRawContent() is { } content &&
            (content.IsDisposed || !content.Content.CanRead ||
             (content.OwningDocument is not null && !ReferenceEquals(content.OwningDocument, this))))
        {
            throw new InvalidOperationException("Original content is disposed or owned by another document.");
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

    internal void ValidateArtifacts(DocumentElement element)
    {
        if (element.OwningDocument is not null && !ReferenceEquals(element.OwningDocument, this))
        {
            throw new InvalidOperationException("An element cannot belong to more than one document.");
        }

        foreach (DocumentArtifact artifact in element.Artifacts)
        {
            if (artifact.OwningDocument is not null && !ReferenceEquals(artifact.OwningDocument, this))
            {
                throw new InvalidOperationException("An artifact cannot belong to more than one document.");
            }
        }

        if (element is DocumentSection section)
        {
            foreach (DocumentElement child in section.Elements)
            {
                ValidateArtifacts(child);
            }
        }
        else if (element is DocumentTableElement table)
        {
            foreach (DocumentTableCell? cell in table.Cells)
            {
                if (cell?.Content is { } content)
                {
                    ValidateArtifacts(content);
                }
            }
        }
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
        if (ReferenceEquals(oldItem, item))
        {
            return;
        }

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
