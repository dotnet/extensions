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
    /// <param name="pageReferences">Optional opaque source-page references.</param>
    /// <param name="sourcePage">Optional originating page and coordinate frame.</param>
    /// <param name="annotations">Optional page-level annotations.</param>
    /// <param name="sourceOutputs">Optional source operations backing the page.</param>
    public DocumentExtractionPage(
        int pageNumber,
        IReadOnlyList<DocumentExtractionObservation> observations,
        string? markdown = null,
        IReadOnlyList<DocumentPageReference>? pageReferences = null,
        DocumentSourcePage? sourcePage = null,
        IReadOnlyList<DocumentAnnotation>? annotations = null,
        IReadOnlyList<DocumentSourceOutput>? sourceOutputs = null)
    {
        _ = Throw.IfLessThanOrEqual(pageNumber, 0, nameof(pageNumber));
        PageNumber = pageNumber;
        Observations = new ReadOnlyCollection<DocumentExtractionObservation>(Throw.IfNull(observations).ToArray());
        PageReferences = new ReadOnlyCollection<DocumentPageReference>((pageReferences ?? Array.Empty<DocumentPageReference>()).ToArray());
        Markdown = markdown;
        SourcePage = sourcePage;
        Annotations = DocumentExtractionObservation.Copy(annotations);
        SourceOutputs = DocumentExtractionObservation.Copy(sourceOutputs);

        if (Observations.Any(static item => item is null) || PageReferences.Any(static item => item is null) ||
            Annotations.Any(static item => item is null) || SourceOutputs.Any(static item => item is null))
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

    /// <summary>Gets opaque source-page references in supplied order and multiplicity.</summary>
    public IReadOnlyList<DocumentPageReference> PageReferences { get; }

    /// <summary>Gets the originating source frame, independently of page assembly order.</summary>
    public DocumentSourcePage? SourcePage { get; }

    /// <summary>Gets page annotations, including optional layout and reported confidence.</summary>
    public IReadOnlyList<DocumentAnnotation> Annotations { get; }

    /// <summary>Gets source operations backing this page; instances may be shared across pages.</summary>
    public IReadOnlyList<DocumentSourceOutput> SourceOutputs { get; }
}
