// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IChatClient"/>.</summary>
public class ChatClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="ChatClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the chat provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the chat provider, if applicable.</param>
    /// <param name="modelId">The ID of the chat model used, if applicable.</param>
    public ChatClientMetadata(string? providerName = null, Uri? providerUri = null, string? modelId = null)
    {
        ModelId = modelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the chat provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the chat provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the model used by this chat provider.</summary>
    /// <remarks>
    /// This value can be null if either the name is unknown or there are multiple possible models associated with this instance.
    /// An individual request may override this value via <see cref="ChatOptions.ModelId"/>.
    /// </remarks>
    public string? ModelId { get; }
}
