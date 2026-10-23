// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Represents one provider observation in page reading order.
/// </summary>
/// <remarks>
/// Provider identifiers are local to one extraction and are used only for duplicate detection and diagnostics.
/// They are not copied into the canonical document as durable identifiers.
/// </remarks>
public abstract class DocumentExtractionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionObservation"/> class.
    /// </summary>
    /// <param name="providerId">The non-empty provider identifier for this observation.</param>
    /// <param name="order">The zero-based reading-order value.</param>
    /// <param name="markdown">The stored markdown supplied for this observation.</param>
    /// <param name="pageReferences">Optional opaque source-page references.</param>
    /// <param name="evidence">Optional opaque evidence associations.</param>
    /// <param name="artifacts">Optional document-owned artifacts.</param>
    /// <param name="annotations">Optional source layout and structured producer information.</param>
    protected DocumentExtractionObservation(
        string providerId,
        int order,
        string markdown,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        IReadOnlyList<DocumentExtractionEvidence>? evidence = null,
        IReadOnlyList<DocumentArtifact>? artifacts = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null)
    {
        ProviderId = Throw.IfNullOrEmpty(providerId);
        _ = Throw.IfLessThan(order, 0, nameof(order));
        Order = order;
        Markdown = Throw.IfNull(markdown);
        PageReferences = Copy(pageReferences);
        Evidence = Copy(evidence);
        Artifacts = Copy(artifacts);
        Annotations = Copy(annotations);

        if (PageReferences.Any(static item => item is null) ||
            Evidence.Any(static item => item is null) ||
            Artifacts.Any(static item => item is null) ||
            Annotations.Any(static item => item is null))
        {
            Throw.ArgumentException(nameof(pageReferences), "Association collections cannot contain null entries.");
        }
    }

    /// <summary>
    /// Gets the provider-local observation identifier.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets the zero-based reading-order value.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the stored provider markdown.
    /// </summary>
    public string Markdown { get; }

    /// <summary>
    /// Gets opaque source-page references in supplied order and multiplicity.
    /// </summary>
    public IReadOnlyList<DocumentPageReference> PageReferences { get; }

    /// <summary>
    /// Gets opaque evidence associations in supplied order.
    /// </summary>
    public IReadOnlyList<DocumentExtractionEvidence> Evidence { get; }

    /// <summary>
    /// Gets document-owned artifacts that are transferred to the finalized document.
    /// </summary>
    public IReadOnlyList<DocumentArtifact> Artifacts { get; }

    /// <summary>Gets structured source layout and supplementary producer information.</summary>
    public IReadOnlyList<DocumentAnnotation> Annotations { get; }

    internal static IReadOnlyList<T> Copy<T>(IEnumerable<T>? values)
        => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToArray());

    internal abstract DocumentElement ToDocumentElement();

    internal void ApplyAssociations(DocumentElement element, IReadOnlyList<DocumentPageReference> pageReferences)
    {
        foreach (DocumentPageReference pageReference in pageReferences)
        {
            element.PageReferences.Add(pageReference);
        }

        foreach (DocumentPageReference pageReference in PageReferences)
        {
            element.PageReferences.Add(pageReference);
        }

        foreach (DocumentExtractionEvidence evidence in Evidence)
        {
            element.Evidence.Add(evidence.Reference);
            if (evidence.Role is not null)
            {
                element.Annotations.Add(new DocumentAnnotation(evidence.Reference.Kind ?? "evidence", evidence.Reference.Value, evidence.Role));
            }
        }

        foreach (DocumentAnnotation annotation in Annotations)
        {
            element.Annotations.Add(annotation);
        }

        foreach (DocumentArtifact artifact in Artifacts)
        {
            element.Artifacts.Add(artifact);
        }
    }
}

/// <summary>
/// Represents extracted paragraph, header, or footer text.
/// </summary>
public sealed class DocumentExtractionText : DocumentExtractionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionText"/> class.
    /// </summary>
    public DocumentExtractionText(
        string providerId,
        int order,
        DocumentTextKind kind,
        string markdown,
        string? text = null,
        int? headerLevel = null,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        IReadOnlyList<DocumentExtractionEvidence>? evidence = null,
        IReadOnlyList<DocumentArtifact>? artifacts = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null)
        : base(providerId, order, markdown, pageReferences, evidence, artifacts, annotations)
    {
        if (kind == DocumentTextKind.Header && headerLevel is not null && (headerLevel < 1 || headerLevel > 10))
        {
            _ = Throw.IfOutOfRange(headerLevel.Value, min: 1, max: 10, nameof(headerLevel));
        }

        Kind = kind;
        Text = text;
        HeaderLevel = headerLevel;
    }

    /// <summary>
    /// Gets the semantic text kind.
    /// </summary>
    public DocumentTextKind Kind { get; }

    /// <summary>
    /// Gets the plain text, when supplied.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the header level, when supplied.
    /// </summary>
    public int? HeaderLevel { get; }

    internal override DocumentElement ToDocumentElement()
    {
        DocumentTextElement element = new(Kind, Markdown, Text)
        {
            HeaderLevel = HeaderLevel
        };
        return element;
    }
}

/// <summary>
/// Represents an extracted image observation.
/// </summary>
public sealed class DocumentExtractionImage : DocumentExtractionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionImage"/> class.
    /// </summary>
    public DocumentExtractionImage(
        string providerId,
        int order,
        string markdown,
        string? text = null,
        ReadOnlyMemory<byte>? content = null,
        string? mediaType = null,
        Uri? source = null,
        string? alternativeText = null,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        IReadOnlyList<DocumentExtractionEvidence>? evidence = null,
        IReadOnlyList<DocumentArtifact>? artifacts = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null)
        : base(providerId, order, markdown, pageReferences, evidence, artifacts, annotations)
    {
        Text = text;
        Content = content;
        MediaType = mediaType;
        Source = source;
        AlternativeText = alternativeText;
    }

    /// <summary>Gets the text associated with the image.</summary>
    public string? Text { get; }

    /// <summary>Gets the optional image bytes.</summary>
    public ReadOnlyMemory<byte>? Content { get; }

    /// <summary>Gets the optional image media type.</summary>
    public string? MediaType { get; }

    /// <summary>Gets the optional opaque image source.</summary>
    public Uri? Source { get; }

    /// <summary>Gets the optional image alternative text.</summary>
    public string? AlternativeText { get; }

    internal override DocumentElement ToDocumentElement()
        => new DocumentImageElement(Markdown, Text)
        {
            Content = Content,
            MediaType = MediaType,
            Source = Source,
            AlternativeText = AlternativeText
        };
}

/// <summary>
/// Represents an extracted table observation.
/// </summary>
public sealed class DocumentExtractionTable : DocumentExtractionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionTable"/> class.
    /// </summary>
    public DocumentExtractionTable(
        string providerId,
        int order,
        string markdown,
        int rowCount,
        int columnCount,
        IReadOnlyList<DocumentExtractionTableCell>? cells = null,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        IReadOnlyList<DocumentExtractionEvidence>? evidence = null,
        IReadOnlyList<DocumentArtifact>? artifacts = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null)
        : base(providerId, order, markdown, pageReferences, evidence, artifacts, annotations)
    {
        _ = Throw.IfLessThan(rowCount, 0, nameof(rowCount));
        _ = Throw.IfLessThan(columnCount, 0, nameof(columnCount));
        RowCount = rowCount;
        ColumnCount = columnCount;
        Cells = Copy(cells);

        if (Cells.Any(static item => item is null))
        {
            Throw.ArgumentException(nameof(cells), "Table cells cannot contain null entries.");
        }
    }

    /// <summary>Gets the row count.</summary>
    public int RowCount { get; }

    /// <summary>Gets the column count.</summary>
    public int ColumnCount { get; }

    /// <summary>Gets the supplied table cells.</summary>
    public IReadOnlyList<DocumentExtractionTableCell> Cells { get; }

    internal override DocumentElement ToDocumentElement()
        => new DocumentTableElement(Markdown, RowCount, ColumnCount);
}

/// <summary>
/// Represents an unknown or provider-specific observation without introducing provider vocabulary into the Documents model.
/// </summary>
public sealed class DocumentExtractionExtension : DocumentExtractionObservation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionExtension"/> class.
    /// </summary>
    public DocumentExtractionExtension(
        string providerId,
        int order,
        string type,
        string markdown,
        string? text = null,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        IReadOnlyList<DocumentExtractionEvidence>? evidence = null,
        IReadOnlyList<DocumentArtifact>? artifacts = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null,
        object? value = null)
        : base(providerId, order, markdown, pageReferences, evidence, artifacts, annotations)
    {
        Type = Throw.IfNullOrEmpty(type);
        Text = text;
        Value = value;
    }

    /// <summary>Gets the opaque extension type name.</summary>
    public string Type { get; }

    /// <summary>Gets optional provider-supplied text associated with the extension.</summary>
    public string? Text { get; }

    /// <summary>Gets structured extension data without converting it to Markdown.</summary>
    public object? Value { get; }

    internal override DocumentElement ToDocumentElement()
    {
        DocumentExtensionElement element = new(Type, Markdown, Value)
        {
            Text = Text
        };
        return element;
    }
}
