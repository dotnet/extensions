// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Accumulates page observations and explicitly finalizes them into a Documents document.
/// </summary>
/// <remarks>
/// Pages may arrive out of order. Finalization sorts pages and observations, rejects duplicate page numbers and
/// provider identifiers, preserves equal text from distinct observations, and preserves page-reference order and
/// multiplicity. Artifacts are owned by the builder until successful finalization and by the result thereafter.
/// </remarks>
public sealed class DocumentExtractionDocumentBuilder : IDisposable
{
    private readonly List<DocumentExtractionPage> _pages = [];
    private readonly bool _captureRawOutput;
    private bool _finalized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionDocumentBuilder"/> class.
    /// </summary>
    /// <param name="identifier">The identifier for the resulting Documents document.</param>
    /// <param name="captureRawOutput">Whether raw output retention is enabled for this request.</param>
    public DocumentExtractionDocumentBuilder(string identifier, bool captureRawOutput = true)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        _captureRawOutput = captureRawOutput;
    }

    /// <summary>Gets the identifier for the resulting document.</summary>
    public string Identifier { get; }

    /// <summary>
    /// Adds a page for finalization.
    /// </summary>
    /// <param name="page">The page to add.</param>
    public void AddPage(DocumentExtractionPage page)
    {
        ThrowIfUnavailable();
        _pages.Add(Throw.IfNull(page));
    }

    /// <summary>
    /// Finalizes the accumulated observations.
    /// </summary>
    /// <returns>The finalized extraction result.</returns>
    public DocumentExtractionResult Build()
    {
        ThrowIfUnavailable();
        _finalized = true;

        List<DocumentExtractionPage> pages = _pages.OrderBy(static page => page.PageNumber).ToList();
        HashSet<DocumentArtifact> artifacts = [];
        Document? document = null;

        try
        {
            if (!CollectArtifacts(pages, artifacts))
            {
                Throw.ArgumentException(nameof(pages), "A document artifact cannot be associated with more than one extraction observation.");
            }

            ValidatePages(pages);
            if (!_captureRawOutput &&
                (pages.SelectMany(EnumerateSources).Any(static source =>
                    source.NativeStatus == DocumentRawContentStatus.Retained || source.ContentStatus == DocumentRawContentStatus.Retained) ||
                 pages.SelectMany(EnumerateAnnotations).Any(static annotation => annotation.GetRawRepresentation() is not null)))
            {
                Throw.ArgumentException(nameof(pages), "The provider returned raw output despite the per-call capture opt-out.");
            }

            document = new Document(Identifier);
            HashSet<string> providerIds = new(StringComparer.Ordinal);

            foreach (DocumentExtractionPage page in pages)
            {
                foreach (DocumentSourceOutput source in page.SourceOutputs)
                {
                    document.AddSourceOutput(source);
                }

                if (page.SourcePage is { } frame)
                {
                    document.AddSourcePage(frame);
                }

                DocumentSection section = new(page.Markdown ?? string.Empty);
                foreach (DocumentPageReference reference in page.PageReferences)
                {
                    section.PageReferences.Add(reference);
                }

                foreach (DocumentAnnotation annotation in page.Annotations)
                {
                    section.Annotations.Add(annotation);
                }

                foreach (DocumentExtractionObservation observation in page.Observations
                    .Select((item, index) => (item, index))
                    .OrderBy(static pair => pair.item.Order)
                    .ThenBy(static pair => pair.index)
                    .Select(static pair => pair.item))
                {
                    DocumentElement element = BuildElement(observation, page.PageReferences, providerIds);
                    section.Elements.Add(element);
                }

                document.Sections.Add(section);
            }

            if (pages.TrueForAll(static page => page.Markdown is not null))
            {
                document.StoredMarkdown = string.Join(Environment.NewLine + Environment.NewLine, pages.Select(static page => page.Markdown));
            }

            DocumentExtractionResult result = new(document, pages);
            document = null;
            return result;
        }
        catch
        {
            DisposeUnattachedArtifacts(artifacts);
            throw;
        }
        finally
        {
            document?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_finalized)
        {
            HashSet<DocumentArtifact> artifacts = [];
            _ = CollectArtifacts(_pages, artifacts);
            DisposeUnattachedArtifacts(artifacts);
        }
    }

    private static DocumentElement BuildElement(
        DocumentExtractionObservation observation,
        IReadOnlyList<DocumentPageReference> pageReferences,
        HashSet<string> providerIds)
    {
        RegisterProviderId(observation, providerIds);

        DocumentElement element = observation.ToDocumentElement();
        observation.ApplyAssociations(element, pageReferences);

        if (observation is DocumentExtractionTable table)
        {
            HashSet<(int Row, int Column)> addresses = [];
            foreach (DocumentExtractionTableCell cell in table.Cells)
            {
                if (cell.Row >= table.RowCount || cell.Column >= table.ColumnCount ||
                    !addresses.Add((cell.Row, cell.Column)))
                {
                    Throw.ArgumentException(nameof(observation), "Table cells must have unique addresses within the table.");
                }

                DocumentTableCell sharedCell = cell.Content is { } observationContent
                    ? new DocumentTableCell(BuildElement(observationContent, pageReferences, providerIds), cell.RowSpan, cell.ColumnSpan, cell.Kind)
                    : new DocumentTableCell(cell.RowSpan, cell.ColumnSpan, cell.Kind);
                foreach (DocumentAnnotation annotation in cell.Annotations)
                {
                    sharedCell.Annotations.Add(annotation);
                }

                ((DocumentTableElement)element).Cells[cell.Row, cell.Column] = sharedCell;
            }
        }

        return element;
    }

    private static void RegisterProviderId(DocumentExtractionObservation observation, HashSet<string> providerIds)
    {
        if (!providerIds.Add(observation.ProviderId))
        {
            Throw.ArgumentException(
                nameof(observation),
                $"Provider observation identifier '{observation.ProviderId}' is repeated. Equal text does not make repeated observations interchangeable.");
        }
    }

    private static void ValidatePages(List<DocumentExtractionPage> pages)
    {
        for (int index = 1; index < pages.Count; index++)
        {
            if (pages[index - 1].PageNumber == pages[index].PageNumber)
            {
                Throw.ArgumentException(nameof(pages), $"Page number {pages[index].PageNumber} is duplicated.");
            }
        }
    }

    private static bool CollectArtifacts(IEnumerable<DocumentExtractionPage> pages, HashSet<DocumentArtifact> artifacts)
    {
        bool unique = true;
        HashSet<DocumentArtifact> semanticArtifacts = [];
        foreach (DocumentExtractionPage page in pages)
        {
            foreach (DocumentExtractionObservation observation in page.Observations)
            {
                unique &= CollectArtifacts(observation, semanticArtifacts);
            }

            foreach (DocumentSourceOutput source in EnumerateSources(page))
            {
                if (source.GetRawContent() is { } content)
                {
                    _ = artifacts.Add(content);
                }
            }
        }

        artifacts.UnionWith(semanticArtifacts);
        return unique;
    }

    private static bool CollectArtifacts(DocumentExtractionObservation observation, HashSet<DocumentArtifact> artifacts)
    {
        bool unique = true;
        foreach (DocumentArtifact artifact in observation.Artifacts)
        {
            unique &= artifacts.Add(artifact);
        }

        if (observation is DocumentExtractionTable table)
        {
            foreach (DocumentExtractionTableCell cell in table.Cells)
            {
                if (cell.Content is { } content)
                {
                    unique &= CollectArtifacts(content, artifacts);
                }
            }
        }

        return unique;
    }

    private static IEnumerable<DocumentAnnotation> EnumerateAnnotations(DocumentExtractionPage page)
    {
        foreach (DocumentAnnotation annotation in page.Annotations)
        {
            yield return annotation;
        }

        foreach (DocumentExtractionObservation observation in page.Observations)
        {
            foreach (DocumentAnnotation annotation in EnumerateAnnotations(observation))
            {
                yield return annotation;
            }
        }
    }

    private static IEnumerable<DocumentAnnotation> EnumerateAnnotations(DocumentExtractionObservation observation)
    {
        foreach (DocumentAnnotation annotation in observation.Annotations)
        {
            yield return annotation;
        }

        if (observation is DocumentExtractionTable table)
        {
            foreach (DocumentExtractionTableCell cell in table.Cells)
            {
                foreach (DocumentAnnotation annotation in cell.Annotations)
                {
                    yield return annotation;
                }

                if (cell.Content is { } content)
                {
                    foreach (DocumentAnnotation annotation in EnumerateAnnotations(content))
                    {
                        yield return annotation;
                    }
                }
            }
        }
    }

    private static IEnumerable<DocumentSourceOutput> EnumerateSources(DocumentExtractionPage page)
    {
        foreach (DocumentSourceOutput source in page.SourceOutputs)
        {
            yield return source;
        }

        if (page.SourcePage?.SourceOutput is { } output)
        {
            yield return output;
        }

        foreach (DocumentAnnotation annotation in EnumerateAnnotations(page))
        {
            if (annotation.SourceOutput is { } source)
            {
                yield return source;
            }

            foreach (DocumentRegion region in annotation.Regions)
            {
                if (region.Page.SourceOutput is { } regionSource)
                {
                    yield return regionSource;
                }
            }
        }
    }

    private static void DisposeUnattachedArtifacts(IEnumerable<DocumentArtifact> artifacts)
    {
        foreach (DocumentArtifact artifact in artifacts)
        {
            if (!artifact.IsDocumentOwned)
            {
                artifact.Dispose();
            }
        }
    }

    private void ThrowIfUnavailable()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DocumentExtractionDocumentBuilder));
        }

        if (_finalized)
        {
            throw new InvalidOperationException("The extraction document builder has already been finalized.");
        }
    }
}
