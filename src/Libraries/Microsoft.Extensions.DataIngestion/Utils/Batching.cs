// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

internal static class Batching
{
    internal static async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync<TMetadata>(IAsyncEnumerable<IngestionChunk<string>> chunks,
        EnricherOptions options,
        string metadataKey,
        ChatMessage systemPrompt,
        ILogger? logger,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TMetadata : notnull
    {
        _ = Throw.IfNull(chunks);

        await foreach (var batch in chunks.Chunk(options.BatchSize).WithCancellation(cancellationToken))
        {
            List<AIContent> contents = new(batch.Length);
            foreach (var chunk in batch)
            {
                contents.Add(new TextContent(chunk.Content));
            }

            try
            {
                ChatResponse<TMetadata[]> response = await options.ChatClient.GetResponseAsync<TMetadata[]>(
                [
                    systemPrompt,
                    new(ChatRole.User, contents)
                ], options.ChatOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (response.Result.Length == contents.Count)
                {
                    for (int i = 0; i < response.Result.Length; i++)
                    {
                        batch[i].Metadata[metadataKey] = response.Result[i];
                    }
                }
                else
                {
                    logger?.UnexpectedResultsCount(response.Result.Length, contents.Count);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // Enricher failures should not fail the whole ingestion pipeline, as they are best-effort enhancements.
                logger?.UnexpectedEnricherFailure(ex);
            }

            foreach (var chunk in batch)
            {
                yield return chunk;
            }
        }
    }
}
