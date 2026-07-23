// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the axis-aligned bounds of a bounding polygon, in the coordinate space defined by the OCR engine.</summary>
/// <param name="Left">The minimum horizontal coordinate.</param>
/// <param name="Top">The minimum vertical coordinate.</param>
/// <param name="Right">The maximum horizontal coordinate.</param>
/// <param name="Bottom">The maximum vertical coordinate.</param>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly record struct OcrBoundingBox(float Left, float Top, float Right, float Bottom);
