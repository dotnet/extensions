// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a code interpreter tool invocation by a hosted service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AICodeInterpreter, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class CodeInterpreterToolResultContent : ToolResultContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeInterpreterToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    public CodeInterpreterToolResultContent(string callId)
        : base(callId)
    {
    }
}
