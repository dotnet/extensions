// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Options for uploading a file to an AI service.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class HostedFileUploadOptions : HostedFileClientOptions
{
    /// <summary>Gets or sets the purpose of the file.</summary>
    /// <remarks>
    /// <para>
    /// If not specified, implementations may default to a provider-specific value
    /// (typically "assistants" or equivalent for code interpreter use).
    /// </para>
    /// <para>
    /// Common values include "assistants", "fine-tune", "batch", and "vision",
    /// but the specific values supported depend on the provider.
    /// </para>
    /// </remarks>
    public string? Purpose { get; set; }
}
