// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        await foreach (var batch in chunks.BufferAsync(options.BatchSize).WithCancellation(cancellationToken))
        {
            List<AIContent> contents = new(batch.Count);
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

    // Code copied from https://github.com/dotnet/reactive/blob/ddf18469a0d9e02fcabe9f606104c81c5822839b/Ix.NET/Source/System.Interactive.Async/System/Linq/Operators/Buffer.cs#L14
    private static IAsyncEnumerable<IList<TSource>> BufferAsync<TSource>(this IAsyncEnumerable<TSource> source, int count)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfLessThanOrEqual(count, 0);

        return CoreAsync(source, count);

        static async IAsyncEnumerable<IList<TSource>> CoreAsync(IAsyncEnumerable<TSource> source, int count,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            List<TSource> buffer = new(count);

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                buffer.Add(item);

                if (buffer.Count == count)
                {
                    yield return buffer;

                    buffer = new List<TSource>(count);
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer;
            }
        }
    }
}
