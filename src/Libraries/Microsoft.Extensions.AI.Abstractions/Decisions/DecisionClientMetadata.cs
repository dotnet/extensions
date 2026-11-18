// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IDecisionClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="DecisionClientMetadata"/> class.</summary>
    /// <param name="providerName">The name of the decision provider, if applicable.</param>
    /// <param name="providerUri">The URL for accessing the decision provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the default model used by this client, if applicable.</param>
    /// <param name="supportedKinds">The question kinds supported by this client, if known.</param>
    public DecisionClientMetadata(
        string? providerName = null,
        Uri? providerUri = null,
        string? defaultModelId = null,
        IReadOnlyList<DecisionKind>? supportedKinds = null)
    {
        ProviderName = providerName;
        ProviderUri = providerUri;
        DefaultModelId = defaultModelId;

        if (supportedKinds is not null)
        {
            DecisionKind[] copy = supportedKinds.Distinct().ToArray();
            foreach (DecisionKind kind in copy)
            {
                if (kind is not (DecisionKind.Binary or DecisionKind.Choice or DecisionKind.Score))
                {
                    Throw.ArgumentException(nameof(supportedKinds), "Supported kinds must be defined decision kinds.");
                }
            }

            SupportedKinds = Array.AsReadOnly(copy);
        }
    }

    /// <summary>Gets the provider name, when known.</summary>
    public string? ProviderName { get; }

    /// <summary>Gets the provider URI, when known.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the default model ID, when known.</summary>
    public string? DefaultModelId { get; }

    /// <summary>Gets the owned supported-kind snapshot, or <see langword="null"/> when unknown.</summary>
    public IReadOnlyList<DecisionKind>? SupportedKinds { get; }
}
