// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Accumulates page observations and explicitly finalizes them into a Documents document.
/// </summary>
/// <remarks>
/// Pages may arrive out of order. Finalization sorts pages and observations, rejects duplicate page numbers and
/// provider identifiers, preserves equal text from distinct observations, and preserves source-reference order and
/// multiplicity. Artifacts are owned by the builder until successful finalization and by the result thereafter.
/// </remarks>
public sealed class DocumentExtractionDocumentBuilder : IDisposable
{
    private readonly List<DocumentExtractionPage> _pages = [];
    private readonly bool _captureRawContent;
    private bool _finalized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionDocumentBuilder"/> class.
    /// </summary>
    /// <param name="identifier">The identifier for the resulting Documents document.</param>
    /// <param name="captureRawContent">Whether available original response content is retained for this request. Native objects are independent of this setting.</param>
    public DocumentExtractionDocumentBuilder(string identifier, bool captureRawContent = false)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        _captureRawContent = captureRawContent;
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
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Success transfers document ownership to the result; failure disposes it in finally and reports cleanup errors.")]
    public DocumentExtractionResult Build()
    {
        ThrowIfUnavailable();
        _finalized = true;

        List<DocumentExtractionPage> pages = _pages.OrderBy(static page => page.PageNumber).ToList();
        HashSet<DocumentArtifact> artifacts = [];
        Document? document = null;
        List<Exception> failures = [];

        try
        {
            if (!CollectArtifacts(pages, artifacts))
            {
                Throw.ArgumentException(nameof(pages), "A document artifact cannot be associated with more than one extraction observation.");
            }

            ValidatePages(pages);
            if (!_captureRawContent &&
                pages.SelectMany(EnumerateSources).Any(static source => source.ContentStatus == DocumentRawContentStatus.Retained))
            {
                Throw.ArgumentException(nameof(pages), "The provider returned original response content despite the per-call content capture opt-out.");
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
                foreach (DocumentSourceReference source in page.Sources)
                {
                    section.Sources.Add(source);
                }

                foreach (KeyValuePair<string, object?> entry in page.Metadata)
                {
                    section.Metadata.Add(entry.Key, entry.Value);
                }

                foreach (DocumentExtractionObservation observation in page.Observations
                    .Select((item, index) => (item, index))
                    .OrderBy(static pair => pair.item.Order)
                    .ThenBy(static pair => pair.index)
                    .Select(static pair => pair.item))
                {
                    DocumentElement element = BuildElement(observation, page.Sources, providerIds);
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
        catch (Exception failure)
        {
            failures.Add(failure);
            DisposeUnattachedArtifacts(artifacts, failures);
        }
        finally
        {
            try
            {
                document?.Dispose();
            }
            catch (Exception cleanupFailure)
            {
                failures.Add(cleanupFailure);
            }
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException(failures);
    }

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Report stream cleanup failures only after attempting every pending artifact.")]
    [SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods", Justification = "Report stream cleanup failures only after attempting every pending artifact.")]
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
            List<Exception> failures = [];
            DisposeUnattachedArtifacts(artifacts, failures);
            if (failures.Count > 0)
            {
                throw new AggregateException(failures);
            }
        }
    }

    private static DocumentElement BuildElement(
        DocumentExtractionObservation observation,
        IReadOnlyList<DocumentSourceReference> pageSources,
        HashSet<string> providerIds)
    {
        RegisterProviderId(observation, providerIds);

        DocumentElement element = observation.ToDocumentElement();
        observation.ApplyAssociations(element, pageSources);

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
                    ? new DocumentTableCell(BuildElement(observationContent, pageSources, providerIds), cell.RowSpan, cell.ColumnSpan, cell.Kind)
                    : new DocumentTableCell(cell.RowSpan, cell.ColumnSpan, cell.Kind);
                foreach (DocumentSourceReference source in cell.Sources)
                {
                    sharedCell.Sources.Add(source);
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

    private static IEnumerable<DocumentSourceReference> EnumerateReferences(DocumentExtractionPage page)
    {
        foreach (DocumentSourceReference source in page.Sources)
        {
            yield return source;
        }

        foreach (DocumentExtractionObservation observation in page.Observations)
        {
            foreach (DocumentSourceReference source in EnumerateReferences(observation))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<DocumentSourceReference> EnumerateReferences(DocumentExtractionObservation observation)
    {
        foreach (DocumentSourceReference source in observation.Sources)
        {
            yield return source;
        }

        if (observation is DocumentExtractionTable table)
        {
            foreach (DocumentExtractionTableCell cell in table.Cells)
            {
                foreach (DocumentSourceReference source in cell.Sources)
                {
                    yield return source;
                }

                if (cell.Content is { } content)
                {
                    foreach (DocumentSourceReference source in EnumerateReferences(content))
                    {
                        yield return source;
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

        foreach (DocumentSourceReference reference in EnumerateReferences(page))
        {
            if (reference.Page.SourceOutput is { } source)
            {
                yield return source;
            }
        }
    }

    private static void DisposeUnattachedArtifacts(IEnumerable<DocumentArtifact> artifacts, List<Exception> failures)
    {
        foreach (DocumentArtifact artifact in artifacts)
        {
            if (!artifact.IsDocumentOwned)
            {
                try
                {
                    artifact.Dispose();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
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
