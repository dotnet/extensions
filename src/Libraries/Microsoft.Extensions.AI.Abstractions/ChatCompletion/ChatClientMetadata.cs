// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IChatClient"/>.</summary>
public class ChatClientMetadata
{
    private static readonly Task<ChatModelMetadata> _emptyModelMetadata = Task.FromResult(new ChatModelMetadata());

    /// <summary>Initializes a new instance of the <see cref="ChatClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the chat provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the chat provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the chat model used by default, if applicable.</param>
    public ChatClientMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
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

    /// <summary>Gets the ID of the default model used by this chat client.</summary>
    /// <remarks>
    /// This value can be null if no default model is set on the corresponding <see cref="IChatClient"/>.
    /// An individual request may override this value via <see cref="ChatOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }

    /// <summary>
    /// Gets metadata for the the default model or a specified model.
    /// </summary>
    /// <param name="modelId">The model identifier. If not set, uses <see cref="DefaultModelId"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that completes with the model metadata.</returns>
    public virtual Task<ChatModelMetadata> GetModelMetadataAsync(string? modelId = null, CancellationToken cancellationToken = default)
        => _emptyModelMetadata;
}
