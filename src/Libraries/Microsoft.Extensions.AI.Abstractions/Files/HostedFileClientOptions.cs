// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Base class for options used with <see cref="IHostedFileClient"/> operations.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class HostedFileClientOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFileClientOptions"/> class.
    /// </summary>
    private protected HostedFileClientOptions()
    {
        // Prevent external derivation
    }

    /// <summary>
    /// Gets or sets a provider-specific scope or location identifier for the file operation.
    /// </summary>
    /// <remarks>
    /// Some providers use scoped storage for files. For example, OpenAI uses containers
    /// to scope code interpreter files. If specified, the operation will target
    /// files within the specified scope.
    /// </remarks>
    public string? Scope { get; set; }

    /// <summary>Gets or sets additional properties for the request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
