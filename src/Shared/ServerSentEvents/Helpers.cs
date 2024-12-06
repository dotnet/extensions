// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
#if !NET
using System.IO;
using System.Runtime.InteropServices;
#endif
using System.Text;
#if !NET
using System.Threading;
using System.Threading.Tasks;
#endif

#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable S2333 // Redundant modifiers should not be used
#pragma warning disable SA1519 // Braces should not be omitted from multi-line child statement
#pragma warning disable LA0001 // Use Microsoft.Shared.Diagnostics.Throws for improved performance
#pragma warning disable LA0002 // Use Microsoft.Shared.Diagnostics.ToInvariantString for improved performance

namespace System.Net.ServerSentEvents
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static class Helpers
    {
        public static void WriteUtf8Number(this IBufferWriter<byte> writer, long value)
        {
#if NET
            const int MaxDecimalDigits = 20;
            Span<byte> buffer = writer.GetSpan(MaxDecimalDigits);
            Debug.Assert(buffer.Length >= MaxDecimalDigits);

            bool success = value.TryFormat(buffer, out int bytesWritten, provider: CultureInfo.InvariantCulture);
            Debug.Assert(success);
            writer.Advance(bytesWritten);
#else
            writer.WriteUtf8String(value.ToString(CultureInfo.InvariantCulture).AsSpan());
#endif
        }

        public static void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            Span<byte> buffer = writer.GetSpan(value.Length);
            Debug.Assert(value.Length <= buffer.Length);
            value.CopyTo(buffer);
            writer.Advance(value.Length);
        }

        public static unsafe void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            Span<byte> buffer = writer.GetSpan(maxByteCount);
            Debug.Assert(maxByteCount <= buffer.Length);
            int bytesWritten;
#if NET
            bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
#else
            fixed (char* chars = value)
            fixed (byte* bytes = buffer)
            {
                bytesWritten = Encoding.UTF8.GetBytes(chars, value.Length, bytes, maxByteCount);
            }
#endif
            writer.Advance(bytesWritten);
        }

        public static bool ContainsLineBreaks(this ReadOnlySpan<char> text) =>
            text.IndexOfAny('\r', '\n') >= 0;

#if !NET

        public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                return new ValueTask(stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
            }
            else
            {
                return WriteAsyncUsingPooledBuffer(stream, buffer, cancellationToken);

                static async ValueTask WriteAsyncUsingPooledBuffer(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                    buffer.Span.CopyTo(sharedBuffer);
                    try
                    {
                        await stream.WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(sharedBuffer);
                    }
                }
            }
        }
#endif

        public static unsafe string Utf8GetString(ReadOnlySpan<byte> bytes)
        {
#if NET
            return Encoding.UTF8.GetString(bytes);
#else
            fixed (byte* ptr = bytes)
            {
                return ptr is null ?
                    string.Empty :
                    Encoding.UTF8.GetString(ptr, bytes.Length);
            }
#endif
        }
    }
}
