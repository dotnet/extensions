// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Represents one page produced by a document extraction client.
/// </summary>
public sealed class DocumentExtractionPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionPage"/> class.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="observations">The observations produced for the page.</param>
    /// <param name="markdown">The exact provider markdown for the page, when available.</param>
    /// <param name="sources">Optional typed source references in supplied order.</param>
    /// <param name="sourcePage">Optional originating page and coordinate frame.</param>
    /// <param name="sourceOutputs">Optional source operations backing the page.</param>
    /// <param name="metadata">Optional provider-specific page metadata. Entries are copied; values remain borrowed.</param>
    public DocumentExtractionPage(
        int pageNumber,
        IReadOnlyList<DocumentExtractionObservation> observations,
        string? markdown = null,
        IReadOnlyList<DocumentSourceReference>? sources = null,
        DocumentSourcePage? sourcePage = null,
        IReadOnlyList<DocumentSourceOutput>? sourceOutputs = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        _ = Throw.IfLessThanOrEqual(pageNumber, 0, nameof(pageNumber));
        PageNumber = pageNumber;
        Observations = new ReadOnlyCollection<DocumentExtractionObservation>(Throw.IfNull(observations).ToArray());
        Sources = DocumentExtractionObservation.Copy(sources);
        Markdown = markdown;
        SourcePage = sourcePage;
        SourceOutputs = DocumentExtractionObservation.Copy(sourceOutputs);
        Metadata = DocumentExtractionObservation.CopyMetadata(metadata);

        if (Observations.Any(static item => item is null) || Sources.Any(static item => item is null) ||
            SourceOutputs.Any(static item => item is null))
        {
            Throw.ArgumentException(nameof(observations), "Page associations cannot contain null entries.");
        }
    }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; }

    /// <summary>Gets observations in the provider-supplied order.</summary>
    public IReadOnlyList<DocumentExtractionObservation> Observations { get; }

    /// <summary>Gets the exact provider markdown for the page.</summary>
    public string? Markdown { get; }

    /// <summary>Gets typed source references in supplied order and multiplicity.</summary>
    public IReadOnlyList<DocumentSourceReference> Sources { get; }

    /// <summary>Gets the originating source frame, independently of page assembly order.</summary>
    public DocumentSourcePage? SourcePage { get; }

    /// <summary>Gets snapshotted provider-specific metadata whose opaque values remain borrowed.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>Gets source operations backing this page; instances may be shared across pages.</summary>
    public IReadOnlyList<DocumentSourceOutput> SourceOutputs { get; }
}
