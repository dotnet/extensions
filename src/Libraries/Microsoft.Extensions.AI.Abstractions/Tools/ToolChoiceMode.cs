// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines modes that controls which if any tool is called by the model.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public enum ToolChoiceMode
{
    /// <summary>
    /// The model will not call any tool and instead generates a message.
    /// </summary>
    None,

    /// <summary>
    /// The model can pick between generating a message or calling one or more tools.
    /// </summary>
    Auto,

    /// <summary>
    /// The model must call one or more tools.
    /// </summary>
    Required
}
