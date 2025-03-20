// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal sealed class AzureAIInferenceChatClientMetadata(string provider, Uri? endpoint, string? modelId)
    : ChatClientMetadata(provider, endpoint, modelId)
{
    // We don't currently have a way to determine per-model metadata for Azure AI Inference
    // so will use fixed metadata for all models.
    private static readonly Task<ChatModelMetadata> _fixedModelMetadata = Task.FromResult(new ChatModelMetadata
    {
        // This is true for most commonly-used models. We consider it useful to default to true, since
        // then GenerateAsync<T> will behave well when used with models that support structured
        // output. Developers who want to use GenerateAsync<T> with models that don't support
        // structured output will need to pass useJsonSchema: false.
        SupportsJsonSchemaResponseFormat = true,
    });

    public override Task<ChatModelMetadata> GetModelMetadataAsync(string? modelId = null, CancellationToken cancellationToken = default)
        => _fixedModelMetadata;
}
