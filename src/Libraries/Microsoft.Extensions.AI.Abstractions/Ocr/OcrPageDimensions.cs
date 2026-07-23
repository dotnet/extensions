// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the width and height of an <see cref="OcrPage"/>, expressed in the page's <see cref="OcrPage.CoordinateUnit"/>.</summary>
/// <param name="Width">The page width.</param>
/// <param name="Height">The page height.</param>
/// <remarks>
/// Together with the page's <see cref="OcrPage.CoordinateUnit"/> and <see cref="OcrPage.CoordinateOrigin"/>, the
/// dimensions let a consumer interpret or normalize the geometry (<see cref="OcrBoundingBox"/> / <see cref="OcrPoint"/>)
/// on the page with engine-agnostic code. For example, dividing a coordinate by the corresponding dimension yields a
/// page-relative [0, 1] value regardless of the native unit.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly record struct OcrPageDimensions(float Width, float Height);
