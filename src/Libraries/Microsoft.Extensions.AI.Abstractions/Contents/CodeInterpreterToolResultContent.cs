// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a code interpreter tool invocation by a hosted service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AICodeInterpreter, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class CodeInterpreterToolResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeInterpreterToolResultContent"/> class.
    /// </summary>
    public CodeInterpreterToolResultContent()
    {
    }

    /// <summary>
    /// Gets or sets the tool call ID that this result corresponds to.
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Gets or sets the output of code interpreter tool.
    /// </summary>
    /// <remarks>
    /// Outputs can include various types of content such as <see cref="HostedFileContent"/> for files,
    /// <see cref="DataContent"/> for binary data, <see cref="TextContent"/> for standard output text,
    /// or other <see cref="AIContent"/> types as supported by the service.
    /// </remarks>
    public IList<AIContent>? Outputs { get; set; }
}
