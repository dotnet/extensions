// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

#pragma warning disable SA1114 // Parameter list should follow declaration

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    private static readonly byte[] _sseEventFieldPrefix = "event: "u8.ToArray();
    private static readonly byte[] _sseDataFieldPrefix = "data: "u8.ToArray();
    private static readonly byte[] _sseIdFieldPrefix = "id: "u8.ToArray();
    private static readonly byte[] _sseLineBreak = Encoding.UTF8.GetBytes(Environment.NewLine);

    /// <summary>
    /// Serializes the specified server-sent events to the provided stream as JSON data.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data payload in the event.</typeparam>
    /// <param name="stream">The UTF-8 stream to write the server-sent events to.</param>
    /// <param name="sseEvents">The events to serialize to the stream.</param>
    /// <param name="options">The options configuring serialization.</param>
    /// <param name="cancellationToken">The token taht can be used to cancel the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async ValueTask SerializeAsSseAsync<T>(
        Stream stream,
        IAsyncEnumerable<SseEvent<T>> sseEvents,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        _ = Throw.IfNull(sseEvents);

        options ??= DefaultOptions;
        options.MakeReadOnly();
        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));

        BufferWriter<byte> bufferWriter = BufferWriterPool.SharedBufferWriterPool.Get();

        try
        {
            // Build a custom Utf8JsonWriter that ignores indentation configuration from JsonSerializerOptions.
            using Utf8JsonWriter writer = new(bufferWriter);

            await foreach (var sseEvent in sseEvents.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                JsonSerializer.Serialize(writer, sseEvent.Data, typeInfo);
#pragma warning disable CA1849 // Call async methods when in an async method
                writer.Flush();
#pragma warning restore CA1849 // Call async methods when in an async method
                Debug.Assert(bufferWriter.WrittenSpan.IndexOf((byte)'\n') == -1, "The buffer writer should not contain any newline characters.");

                if (sseEvent.EventType is { } eventType)
                {
                    await stream.WriteAsync(_sseEventFieldPrefix, cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(eventType), cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);
                }

                await stream.WriteAsync(_sseDataFieldPrefix, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(
#if NET
                    bufferWriter.WrittenMemory,
#else
                    bufferWriter.WrittenMemory.ToArray(),
#endif
                    cancellationToken).ConfigureAwait(false);

                await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);

                if (sseEvent.Id is { } id)
                {
                    await stream.WriteAsync(_sseIdFieldPrefix, cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(id), cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);
                }

                await stream.WriteAsync(_sseLineBreak, cancellationToken).ConfigureAwait(false);

                bufferWriter.Reset();
                writer.Reset();
            }
        }
        finally
        {
            BufferWriterPool.SharedBufferWriterPool.Return(bufferWriter);
        }
    }

#if !NET
    private static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        => stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
#endif
}
