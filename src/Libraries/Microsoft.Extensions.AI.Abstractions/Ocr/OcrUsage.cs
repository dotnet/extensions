// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents usage details associated with an OCR request.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrUsage
{
    /// <summary>Gets or sets the number of pages processed by the request, when known.</summary>
    public int? PagesProcessed { get; set; }

    /// <summary>Gets or sets any additional provider-specific usage details.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
