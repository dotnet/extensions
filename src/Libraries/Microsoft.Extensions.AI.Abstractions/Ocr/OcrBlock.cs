// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a positioned layout block, such as a paragraph, heading, or figure.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrBlock
{
    /// <summary>Initializes a new instance of the <see cref="OcrBlock"/> class.</summary>
    /// <param name="text">The text content of the block.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public OcrBlock(string text)
    {
        Text = Throw.IfNull(text);
    }

    /// <summary>Gets the text content of the block.</summary>
    public string Text { get; }

    /// <summary>Gets or sets the kind of block, for example <c>paragraph</c>, <c>title</c>, or <c>figure</c>.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the region of the page the block occupies, when the engine provides geometry.</summary>
    public OcrBoundingRegion? BoundingRegion { get; set; }

    /// <summary>Gets or sets the confidence for the block in the range [0, 1], when available.</summary>
    public double? Confidence { get; set; }
}
