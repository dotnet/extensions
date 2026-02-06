// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Options for listing files from an AI service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class HostedFileListOptions : HostedFileClientOptions
{
    /// <summary>Gets or sets a purpose filter.</summary>
    /// <remarks>
    /// If specified, only files with the given purpose will be returned.
    /// </remarks>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the maximum number of files to return.</summary>
    /// <remarks>
    /// If not specified, the provider's default limit will be used.
    /// </remarks>
    public int? Limit { get; set; }
}
