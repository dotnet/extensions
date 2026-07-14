// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents one page of structured OCR output.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrPage
{
    /// <summary>Initializes a new instance of the <see cref="OcrPage"/> class.</summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="markdown">The structured markdown for this page.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="markdown"/> is <see langword="null"/>.</exception>
    public OcrPage(int pageNumber, string markdown)
    {
        PageNumber = pageNumber;
        Markdown = Throw.IfNull(markdown);
    }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; }

    /// <summary>Gets the structured markdown for this page, with headings, tables, and reading order preserved.</summary>
    public string Markdown { get; }

    /// <summary>Gets or sets the tables extracted from this page.</summary>
    public IReadOnlyList<OcrTable> Tables { get; set; } = [];

    /// <summary>Gets or sets the layout blocks with bounding regions and confidence, when the engine provides them.</summary>
    public IReadOnlyList<OcrBlock> Blocks { get; set; } = [];

    /// <summary>Gets or sets the images or figures extracted from this page, when requested and the engine provides them.</summary>
    public IReadOnlyList<OcrImage> Images { get; set; } = [];

    /// <summary>Gets or sets the page-level confidence in the range [0, 1], when available.</summary>
    public double? Confidence { get; set; }

    /// <summary>Gets or sets any additional properties associated with the page.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
