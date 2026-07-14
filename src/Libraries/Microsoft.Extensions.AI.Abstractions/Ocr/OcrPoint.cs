// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a single vertex of a bounding polygon, in the coordinate space defined by the OCR engine.</summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly record struct OcrPoint(float X, float Y);
