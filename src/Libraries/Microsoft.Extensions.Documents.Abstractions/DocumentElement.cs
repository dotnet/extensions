// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents an element in a structured document.
/// </summary>
public abstract class DocumentElement
{
    private readonly Guid _localId = Guid.NewGuid();
    private string _markdown;
    private Dictionary<string, object?>? _metadata;
    private object? _parent;
    private Document? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentElement"/> class.
    /// </summary>
    /// <param name="markdown">The stored markdown representation of the element.</param>
    protected DocumentElement(string markdown)
    {
        _markdown = Throw.IfNull(markdown);
        Artifacts = new DocumentArtifactCollection(this);
        Sources = new DocumentSourceReferenceCollection(() => _document);
    }

    /// <summary>
    /// Gets the process-local identity of this element.
    /// </summary>
    /// <remarks>
    /// This value is not a durable serialization identifier and has no provider-specific meaning.
    /// </remarks>
    internal Guid LocalId => _localId;

    /// <summary>
    /// Gets the current structural address of this element within its owning document.
    /// </summary>
    /// <remarks>
    /// The address is local to the current in-memory hierarchy and changes when the hierarchy is edited.
    /// It is <see langword="null"/> while the element is not attached to a document.
    /// </remarks>
    internal string? Address => _document?.GetAddress(this);

    /// <summary>
    /// Gets the section that directly contains this element, or <see langword="null"/> for a root section or a table cell.
    /// </summary>
    public DocumentSection? ParentSection => _parent as DocumentSection;

    /// <summary>
    /// Gets or sets the stored markdown representation of this element.
    /// </summary>
    /// <remarks>
    /// For sections and tables, this value is intentionally distinct from the semantic markdown projection returned by
    /// <see cref="ToMarkdown"/>.
    /// </remarks>
    public string StoredMarkdown
    {
        get => _markdown;
        set
        {
            string markdown = Throw.IfNull(value);
            if (!string.Equals(_markdown, markdown, StringComparison.Ordinal))
            {
                _document?.ThrowIfDisposed();
                _markdown = markdown;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets or sets the plain-text content associated with this element.
    /// </summary>
    public string? Text
    {
        get;
        set
        {
            if (!string.Equals(field, value, StringComparison.Ordinal))
            {
                _document?.ThrowIfDisposed();
                field = value;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this element has metadata.
    /// </summary>
    internal bool HasMetadata => _metadata is { Count: > 0 };

    /// <summary>
    /// Gets the metadata associated with this element.
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata ??= new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Gets optional page and region associations, excluded from Markdown projection.</summary>
    public IList<DocumentSourceReference> Sources { get; }

    /// <summary>
    /// Gets the artifacts associated with this element.
    /// </summary>
    public IList<DocumentArtifact> Artifacts { get; }

    /// <summary>
    /// Gets the stored markdown representation of this element.
    /// </summary>
    /// <returns>The stored markdown.</returns>
    public virtual string GetMarkdown() => _markdown;

    /// <summary>
    /// Gets a markdown projection of the semantic content of this element.
    /// </summary>
    /// <returns>The semantic markdown projection.</returns>
    public virtual string ToMarkdown() => _markdown;

    internal Document? OwningDocument => _document;

    internal object? Parent => _parent;

    internal void Attach(object parent, Document? document)
    {
        if (_parent is not null)
        {
            throw new InvalidOperationException("The element is already owned by a document hierarchy.");
        }

        _parent = parent;

        try
        {
            SetDocument(document);
        }
        catch
        {
            _parent = null;
            throw;
        }
    }

    internal void Detach(object parent)
    {
        if (!ReferenceEquals(_parent, parent))
        {
            throw new InvalidOperationException("The element is not owned by the specified parent.");
        }

        SetDocument(null);
        _parent = null;
    }

    internal void SetDocument(Document? document)
    {
        if (ReferenceEquals(_document, document))
        {
            return;
        }

        if (_document is not null && document is not null)
        {
            throw new InvalidOperationException("An element cannot belong to more than one document.");
        }

        if (document is null)
        {
            _document = null;

            if (this is DocumentSection detachedSection)
            {
                foreach (DocumentElement element in detachedSection.Elements)
                {
                    element.SetDocument(null);
                }
            }
            else if (this is DocumentTableElement detachedTable)
            {
                foreach (DocumentTableCell? cell in detachedTable.Cells)
                {
                    cell?.Content?.SetDocument(null);
                }
            }

            return;
        }

        document.ValidateArtifacts(this);
        document.ValidateSources(Document.EnumerateSources(this));
        _document = document;

        try
        {
            document.RegisterArtifacts(this);

            if (this is DocumentSection section)
            {
                foreach (DocumentElement element in section.Elements)
                {
                    element.SetDocument(document);
                }
            }
            else if (this is DocumentTableElement table)
            {
                foreach (DocumentTableCell? cell in table.Cells)
                {
                    cell?.Content?.SetDocument(document);
                }
            }
        }
        catch
        {
            _document = null;
            throw;
        }
    }

    internal bool Contains(DocumentElement element)
    {
        if (ReferenceEquals(this, element))
        {
            return true;
        }

        if (this is DocumentSection section)
        {
            foreach (DocumentElement child in section.Elements)
            {
                if (child.Contains(element))
                {
                    return true;
                }
            }
        }
        else if (this is DocumentTableElement table)
        {
            foreach (DocumentTableCell? cell in table.Cells)
            {
                if (cell?.Content is { } content && content.Contains(element))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal void NotifyMutation() => _document?.NotifyMutation();

}

/// <summary>
/// Represents a section in a document. Sections can be nested in the ordered element collection.
/// </summary>
public sealed class DocumentSection : DocumentElement
{
    private readonly DocumentElementCollection _elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSection"/> class.
    /// </summary>
    /// <param name="markdown">The stored markdown representation of the section.</param>
    public DocumentSection(string markdown)
        : base(markdown)
    {
        _elements = new DocumentElementCollection(this);
    }

    /// <summary>
    /// Gets the ordered elements in this section, including nested sections.
    /// </summary>
    public IList<DocumentElement> Elements => _elements;

    /// <inheritdoc/>
    public override string ToMarkdown()
        => Elements.Count == 0 ? StoredMarkdown : string.Join(Environment.NewLine, Elements.Select(static element => element.ToMarkdown()));
}

/// <summary>
/// Identifies the semantic role of a text element.
/// </summary>
public enum DocumentTextKind
{
    /// <summary>Paragraph content.</summary>
    Paragraph,

    /// <summary>Header content.</summary>
    Header,

    /// <summary>Footer content.</summary>
    Footer
}

/// <summary>
/// Represents paragraph, header, or footer text.
/// </summary>
public sealed class DocumentTextElement : DocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTextElement"/> class.
    /// </summary>
    /// <param name="kind">The semantic role of the text.</param>
    /// <param name="markdown">The stored markdown representation.</param>
    /// <param name="text">The plain-text content.</param>
    public DocumentTextElement(DocumentTextKind kind, string markdown, string? text = null)
        : base(markdown)
    {
        Kind = kind;
        Text = text;
    }

    /// <summary>
    /// Gets the semantic role of this text element.
    /// </summary>
    public DocumentTextKind Kind { get; }

    /// <summary>
    /// Gets or sets the header level. Values must be between 1 and 10 when specified.
    /// </summary>
    /// <remarks>
    /// This property is meaningful for elements whose <see cref="Kind"/> is <see cref="DocumentTextKind.Header"/>.
    /// </remarks>
    public int? HeaderLevel
    {
        get;
        set
        {
            if (value is not null && (value.Value < 1 || value.Value > 10))
            {
                _ = Throw.IfOutOfRange(value.Value, min: 1, max: 10, nameof(value));
            }

            if (field != value)
            {
                OwningDocument?.ThrowIfDisposed();
                field = value;
                NotifyMutation();
            }
        }
    }
}

/// <summary>
/// Represents an image in a document.
/// </summary>
public sealed class DocumentImageElement : DocumentElement
{
    private ReadOnlyMemory<byte>? _content;
    private string? _mediaType;
    private Uri? _source;
    private string? _alternativeText;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentImageElement"/> class.
    /// </summary>
    /// <param name="markdown">The stored markdown representation.</param>
    /// <param name="text">The plain-text content associated with the image.</param>
    public DocumentImageElement(string markdown, string? text = null)
        : base(markdown)
    {
        Text = text;
    }

    /// <summary>
    /// Gets or sets the binary content of the image.
    /// </summary>
    public ReadOnlyMemory<byte>? Content
    {
        get => _content;
        set
        {
            if (!Nullable.Equals(_content, value))
            {
                OwningDocument?.ThrowIfDisposed();
                _content = value;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets or sets the media type of the image.
    /// </summary>
    public string? MediaType
    {
        get => _mediaType;
        set
        {
            if (!string.Equals(_mediaType, value, StringComparison.Ordinal))
            {
                OwningDocument?.ThrowIfDisposed();
                _mediaType = value;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets or sets an opaque source reference for the image.
    /// </summary>
    public Uri? Source
    {
        get => _source;
        set
        {
            if (!Equals(_source, value))
            {
                OwningDocument?.ThrowIfDisposed();
                _source = value;
                NotifyMutation();
            }
        }
    }

    /// <summary>
    /// Gets or sets the alternative text for the image.
    /// </summary>
    public string? AlternativeText
    {
        get => _alternativeText;
        set
        {
            if (!string.Equals(_alternativeText, value, StringComparison.Ordinal))
            {
                OwningDocument?.ThrowIfDisposed();
                _alternativeText = value;
                NotifyMutation();
            }
        }
    }

    /// <inheritdoc/>
    public override string ToMarkdown()
        => Source is null ? StoredMarkdown : $"![{AlternativeText ?? string.Empty}]({Source})";
}

/// <summary>
/// Represents an extension or unknown element without adding provider-specific vocabulary to the common model.
/// </summary>
public sealed class DocumentExtensionElement : DocumentElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtensionElement"/> class.
    /// </summary>
    /// <param name="type">An opaque extension type name.</param>
    /// <param name="markdown">The stored markdown representation.</param>
    /// <param name="value">An opaque provider-owned value.</param>
    public DocumentExtensionElement(string type, string markdown, object? value = null)
        : base(markdown)
    {
        Type = Throw.IfNullOrEmpty(type);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque extension type name.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the opaque extension value.
    /// </summary>
    public object? Value { get; }
}

internal sealed class DocumentElementCollection : Collection<DocumentElement>
{
    private readonly DocumentSection _owner;

    internal DocumentElementCollection(DocumentSection owner) => _owner = owner;

    protected override void InsertItem(int index, DocumentElement item)
    {
        _ = Throw.IfNull(item);

        _owner.OwningDocument?.ThrowIfDisposed();
        ValidateCanAdd(item);
        item.Attach(_owner, _owner.OwningDocument);

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

    protected override void SetItem(int index, DocumentElement item)
    {
        _ = Throw.IfNull(item);

        _owner.OwningDocument?.ThrowIfDisposed();
        DocumentElement oldItem = this[index];
        if (ReferenceEquals(oldItem, item))
        {
            return;
        }

        ValidateCanAdd(item);
        item.Attach(_owner, _owner.OwningDocument);

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
        _owner.OwningDocument?.ThrowIfDisposed();
        DocumentElement item = this[index];
        item.Detach(_owner);
        base.RemoveItem(index);
        _owner.NotifyMutation();
    }

    protected override void ClearItems()
    {
        _owner.OwningDocument?.ThrowIfDisposed();
        foreach (DocumentElement item in this)
        {
            item.Detach(_owner);
        }

        base.ClearItems();
        _owner.NotifyMutation();
    }

    private void ValidateCanAdd(DocumentElement item)
    {
        if (ReferenceEquals(item, _owner) || item.Contains(_owner))
        {
            throw new InvalidOperationException("Adding the element would create a cycle.");
        }

        if (item.Parent is not null)
        {
            throw new InvalidOperationException("The element is already owned by a document hierarchy.");
        }
    }
}

internal sealed class DocumentArtifactCollection : Collection<DocumentArtifact>
{
    private readonly DocumentElement _owner;

    internal DocumentArtifactCollection(DocumentElement owner) => _owner = owner;

    protected override void InsertItem(int index, DocumentArtifact item)
    {
        _ = Throw.IfNull(item);

        _owner.OwningDocument?.ThrowIfDisposed();
        _owner.OwningDocument?.RegisterArtifact(item);
        base.InsertItem(index, item);
        _owner.NotifyMutation();
    }

    protected override void SetItem(int index, DocumentArtifact item)
    {
        _ = Throw.IfNull(item);

        _owner.OwningDocument?.ThrowIfDisposed();
        if (ReferenceEquals(this[index], item))
        {
            return;
        }

        _owner.OwningDocument?.RegisterArtifact(item);
        base.SetItem(index, item);
        _owner.NotifyMutation();
    }

    protected override void RemoveItem(int index)
    {
        _owner.OwningDocument?.ThrowIfDisposed();
        base.RemoveItem(index);
        _owner.NotifyMutation();
    }

    protected override void ClearItems()
    {
        _owner.OwningDocument?.ThrowIfDisposed();
        base.ClearItems();
        _owner.NotifyMutation();
    }
}
