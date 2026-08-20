// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Specifies the OpenTelemetry GenAI semantic convention representation to emit.</summary>
[Experimental(DiagnosticIds.Experiments.AIOpenTelemetryGenAISemanticConvention, UrlFormat = DiagnosticIds.UrlFormat)]
public enum OpenTelemetryGenAISemanticConvention
{
    /// <summary>
    /// Emit the representation defined by the OpenTelemetry GenAI semantic conventions v1.36.
    /// </summary>
    Version1_36 = 0,

    /// <summary>Emit the latest experimental representation supported by this package.</summary>
    LatestExperimental = 1,
}
