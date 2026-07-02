// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options to configure an OCR request.</summary>
/// <remarks>
/// Normalized options common to engines, plus an <see cref="AdditionalProperties"/> bag for
/// provider-specific settings, mirroring <c>ChatOptions</c>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OcrOptions
{
    /// <summary>Gets or sets the model or deployment identifier to use for this request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets a value indicating whether the engine should include rendered images inline, when supported.</summary>
    public bool IncludeImages { get; set; }

    /// <summary>Gets or sets any additional provider-specific request settings.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="OcrOptions"/> instance.</summary>
    /// <returns>A shallow clone of the options instance.</returns>
    public OcrOptions Clone() =>
        new()
        {
            ModelId = ModelId,
            IncludeImages = IncludeImages,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };
}
