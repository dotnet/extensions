// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal sealed class AzureAIInferenceChatClientMetadata(string provider, Uri? endpoint, string? modelId)
    : ChatClientMetadata(provider, endpoint, modelId)
{
    private const int MaxMetadataCacheEntries = 1000;
    private static readonly ConcurrentDictionary<string, ChatModelMetadata> _metadataByModelId = new();

    // See https://learn.microsoft.com/en-us/azure/ai-foundry/model-inference/concepts/models
    private static readonly string[] _supportedStructuredOutputModelIdPrefixes =
    [
        "gpt-4.5",
        "o3-mini",
        "o1",
        "gpt-4o",
        "gpt-4o-mini",
        "AI21-Jamba-1.5-Mini",
        "AI21-Jamba-1.5-Large",
    ];

    // These are model IDs that would otherwise match one of the prefixes above,
    // but need to be special-cased because they don't support structured output.
    private static readonly string[] _unsupportedStructuredOutputModelIdPrefixes =
    [
        "o1-mini",
    ];

    public override Task<ChatModelMetadata> GetModelMetadataAsync(string? modelId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            modelId = DefaultModelId;
        }

        // There could be a brief race here and we could add more than the intended max items to the cache.
        // That wouldn't be a problem since it's an arbitrary limit to mitigate memory leaks if the application
        // is doing something very strange (generating distinct model IDs per call).
        var result = _metadataByModelId.Count > MaxMetadataCacheEntries
            ? (_metadataByModelId.TryGetValue(modelId ?? string.Empty, out var entry) ? entry : CreateModelMetadata(modelId))
            : _metadataByModelId.GetOrAdd(modelId ?? string.Empty, CreateModelMetadata);

        return Task.FromResult(result);
    }

    private static bool? SupportsNativeJsonSchema(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return null;
        }

        // The following is not guaranteed to be correct because
        // (1) The service may introduce further models that support native JSON schema,
        // (2) The service may introduce new models that don't support native JSON schema
        //     but have names that have the same prefix as ones that do. For example
        //     they could theoretically introduce an "gpt-4o-micro" model that doesn't
        //     support it, but that would match our check for "gpt-4o-*"
        // It's up to application developers to override the default if needed.

        foreach (var prefix in _unsupportedStructuredOutputModelIdPrefixes)
        {
            if (MatchesPrefix(modelId!, prefix))
            {
                return null; // It might start suporting it in the future
            }
        }

        foreach (var prefix in _supportedStructuredOutputModelIdPrefixes)
        {
            if (MatchesPrefix(modelId!, prefix))
            {
                return true;
            }
        }

        // Any other model *might* start supporting it in the future, so we never return false here
        return null;
    }

    private static bool MatchesPrefix(string modelId, string prefix)
    {
        if (!modelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return modelId.Length == prefix.Length || modelId[prefix.Length] == '-';
    }

    private ChatModelMetadata CreateModelMetadata(string? modelId) => new ChatModelMetadata
    {
        SupportsNativeJsonSchema = SupportsNativeJsonSchema(modelId),
    };
}
