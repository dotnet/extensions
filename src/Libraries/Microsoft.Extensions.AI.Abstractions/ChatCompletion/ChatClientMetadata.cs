// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IChatClient"/>.</summary>
public class ChatClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="ChatClientMetadata"/> class.</summary>
    /// <param name="providerName">The name of the chat completion provider, if applicable.</param>
    /// <param name="providerUri">The URL for accessing the chat completion provider, if applicable.</param>
    /// <param name="modelId">The id of the chat completion model used, if applicable.</param>
    public ChatClientMetadata(string? providerName = null, Uri? providerUri = null, string? modelId = null)
    {
        ModelId = modelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the chat completion provider.</summary>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the chat completion provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the id of the model used by this chat completion provider.</summary>
    /// <remarks>This may be null if either the name is unknown or there are multiple possible models associated with this instance.</remarks>
    public string? ModelId { get; }
}
