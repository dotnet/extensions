// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents usage details associated with an OCR request.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrUsage
{
    /// <summary>Gets or sets the number of pages processed by the request, when known.</summary>
    public int? PagesProcessed { get; set; }

    /// <summary>Gets or sets the number of input tokens consumed, when the engine reports token usage.</summary>
    /// <remarks>Typically reported only by vision-LLM OCR paths; classic OCR engines usually leave this <see langword="null"/>.</remarks>
    public int? InputTokenCount { get; set; }

    /// <summary>Gets or sets the number of output tokens produced, when the engine reports token usage.</summary>
    public int? OutputTokenCount { get; set; }

    /// <summary>Gets or sets the total number of tokens (input plus output), when the engine reports token usage.</summary>
    public int? TotalTokenCount { get; set; }

    /// <summary>Gets or sets any additional provider-specific usage details.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
