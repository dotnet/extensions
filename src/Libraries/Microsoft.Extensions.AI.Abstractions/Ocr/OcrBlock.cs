// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a positioned layout block, such as a paragraph, heading, or figure.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrBlock : OcrElement
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

    /// <summary>Gets or sets the kind of block, for example <see cref="OcrBlockKind.Paragraph"/>, <see cref="OcrBlockKind.Title"/>, or <see cref="OcrBlockKind.Figure"/>.</summary>
    public OcrBlockKind? Kind { get; set; }
}
