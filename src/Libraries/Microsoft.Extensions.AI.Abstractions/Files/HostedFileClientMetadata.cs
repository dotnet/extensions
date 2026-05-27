// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides metadata about an <see cref="IHostedFileClient"/>.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedFileClientMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFileClientMetadata"/> class.
    /// </summary>
    /// <param name="providerName">The name of the file client provider, if applicable.</param>
    /// <param name="providerUri">The URI of the provider's endpoint, if applicable.</param>
    public HostedFileClientMetadata(string? providerName = null, Uri? providerUri = null)
    {
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the file client provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the name of the company or organization that provides the
    /// underlying file storage, such as "openai", "anthropic", or "google".
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URI of the provider's endpoint.</summary>
    public Uri? ProviderUri { get; }
}
