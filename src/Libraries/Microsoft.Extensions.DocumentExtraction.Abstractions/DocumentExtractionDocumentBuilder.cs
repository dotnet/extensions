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
    private bool _finalized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionDocumentBuilder"/> class.
    /// </summary>
    /// <param name="identifier">The identifier for the resulting Documents document.</param>
    public DocumentExtractionDocumentBuilder(string identifier)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
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
            document = new Document(Identifier);
            HashSet<string> providerIds = new(StringComparer.Ordinal);

            foreach (DocumentExtractionPage page in pages)
            {
                if (page.Observations.Count == 0)
                {
                    continue;
                }

                DocumentSection section = new(page.Markdown ?? "page");
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

                DocumentElement content = BuildElement(cell.Content, pageReferences, providerIds);
                ((DocumentTableElement)element).Cells.SetContent(cell.Row, cell.Column, content);
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
        foreach (DocumentExtractionPage page in pages)
        {
            foreach (DocumentExtractionObservation observation in page.Observations)
            {
                unique &= CollectArtifacts(observation, artifacts);
            }
        }

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
                unique &= CollectArtifacts(cell.Content, artifacts);
            }
        }

        return unique;
    }

    private static void DisposeUnattachedArtifacts(IEnumerable<DocumentArtifact> artifacts)
    {
        foreach (DocumentArtifact artifact in artifacts)
        {
            artifact.Dispose();
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
