// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IHostedConversationClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedConversationClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="HostedConversationClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the hosted conversation provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the hosted conversation provider, if applicable.</param>
    public HostedConversationClientMetadata(string? providerName = null, Uri? providerUri = null)
    {
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the hosted conversation provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the hosted conversation provider.</summary>
    public Uri? ProviderUri { get; }
}
