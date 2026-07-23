// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an image or figure extracted from a page during OCR.</summary>
/// <remarks>
/// Populated when the engine supports it and images are present. Every
/// member is optional so each implementer fills what it can provide: document-native engines (for
/// example Mistral OCR inline images, or Azure Document Intelligence figures) populate <see cref="Content"/>
/// with the rendered image bytes, whereas a vision-LLM transcriber that cannot emit bytes may instead
/// populate only <see cref="Caption"/>. This lets one shape serve both provider archetypes.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrImage : OcrElement
{
    /// <summary>Gets or sets the rendered image bytes, when the engine returns them.</summary>
    public DataContent? Content { get; set; }

    /// <summary>Gets or sets a caption or description of the image, when available.</summary>
    public string? Caption { get; set; }
}
