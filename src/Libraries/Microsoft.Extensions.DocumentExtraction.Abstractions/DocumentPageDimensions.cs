// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Represents the width and height of an <see cref="DocumentPage"/>, expressed in the page's <see cref="DocumentPage.CoordinateUnit"/>.</summary>
/// <param name="Width">The page width.</param>
/// <param name="Height">The page height.</param>
/// <remarks>
/// Together with the page's <see cref="DocumentPage.CoordinateUnit"/> and <see cref="DocumentPage.CoordinateOrigin"/>, the
/// dimensions let a consumer interpret or normalize the geometry (<see cref="DocumentBoundingBox"/> / <see cref="DocumentPoint"/>)
/// on the page with engine-agnostic code. For example, dividing a coordinate by the corresponding dimension yields a
/// page-relative [0, 1] value regardless of the native unit.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly record struct DocumentPageDimensions(float Width, float Height);
