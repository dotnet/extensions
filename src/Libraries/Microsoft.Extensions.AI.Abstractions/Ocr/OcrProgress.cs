// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents progress reported during a long-running OCR request.</summary>
/// <remarks>
/// Engines that poll a long-running operation (such as Azure Document Intelligence) report pages as
/// they complete; synchronous engines (such as Mistral OCR) report a single terminal update.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrProgress
{
    /// <summary>Gets or sets the number of pages processed so far, when known.</summary>
    public int? PagesProcessed { get; set; }

    /// <summary>Gets or sets the total number of pages, when known.</summary>
    public int? TotalPages { get; set; }

    /// <summary>Gets or sets a human-readable status for the operation, when available.</summary>
    public string? Status { get; set; }
}
