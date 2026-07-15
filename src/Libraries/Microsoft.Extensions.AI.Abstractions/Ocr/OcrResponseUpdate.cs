// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a single streaming update from an <see cref="IOcrClient"/>.</summary>
/// <remarks>
/// <para>
/// An <see cref="IOcrClient.ExtractStreamingAsync"/> request produces a sequence of
/// <see cref="OcrResponseUpdate"/> instances. A typical engine emits one update per page as that page
/// finishes (carrying the completed <see cref="Page"/>), optionally interleaved with progress-only updates
/// (<see cref="PagesProcessed"/>, <see cref="TotalPages"/>, <see cref="Status"/>) for long-running
/// operations such as Azure Document Intelligence. A synchronous engine may emit a single terminal update.
/// </para>
/// <para>
/// The relationship between <see cref="OcrResult"/> and <see cref="OcrResponseUpdate"/> is codified in
/// <see cref="OcrResponseUpdateExtensions.ToOcrResultAsync"/>, which reassembles a stream of updates into a
/// single <see cref="OcrResult"/>. The conversion can be slightly lossy: for example, only one
/// <see cref="RawRepresentation"/> slot is available on the assembled <see cref="OcrResult"/>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public class OcrResponseUpdate
{
    /// <summary>Initializes a new instance of the <see cref="OcrResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public OcrResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OcrResponseUpdate"/> class with the page completed in this update.</summary>
    /// <param name="page">The page produced in this update.</param>
    public OcrResponseUpdate(OcrPage? page)
    {
        Page = page;
    }

    /// <summary>Gets or sets the page produced in this update, when the update carries a completed page.</summary>
    /// <remarks>Progress-only updates leave this <see langword="null"/>.</remarks>
    public OcrPage? Page { get; set; }

    /// <summary>Gets or sets the number of pages processed so far, when known.</summary>
    public int? PagesProcessed { get; set; }

    /// <summary>Gets or sets the total number of pages, when known.</summary>
    public int? TotalPages { get; set; }

    /// <summary>Gets or sets a human-readable status for the operation, when available.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the model or deployment identifier that served the request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets usage details associated with the request, when reported.</summary>
    /// <remarks>Usage is typically carried on a terminal update once the full document has been processed.</remarks>
    public OcrUsage? Usage { get; set; }

    /// <summary>Gets or sets the provider-native object underlying this update.</summary>
    /// <remarks>
    /// If an <see cref="OcrResponseUpdate"/> is created to represent an underlying object from another object
    /// model, this property can store that original object. This can be useful for debugging or for enabling
    /// a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
