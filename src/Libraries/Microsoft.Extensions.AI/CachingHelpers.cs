// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
#if NET
using System.Threading;
using System.Threading.Tasks;
#endif

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1502 // Element should not be on a single line

namespace Microsoft.Extensions.AI;

/// <summary>Provides internal helpers for implementing caching services.</summary>
internal static class CachingHelpers
{
    /// <summary>Computes a default cache key for the specified parameters.</summary>
    /// <param name="values">The data with which to compute the key.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    public static string GetCacheKey(ReadOnlySpan<object?> values, JsonSerializerOptions serializerOptions)
    {
        Debug.Assert(serializerOptions is not null, "Expected serializer options to be non-null");
        Debug.Assert(serializerOptions!.IsReadOnly, "Expected serializer options to already be read-only.");

        // The complete JSON representation is excessively long for a cache key, duplicating much of the content
        // from the value. So we use a hash of it as the default key, and we rely on collision resistance for security purposes.
        // If a collision occurs, we'd serve the cached LLM response for a potentially unrelated prompt, leading to information
        // disclosure. Use of SHA256 is an implementation detail and can be easily swapped in the future if needed, albeit
        // invalidating any existing cache entries that may exist in whatever IDistributedCache was in use.

#if NET
        IncrementalHashStream? stream = IncrementalHashStream.ThreadStaticInstance ?? new();
        IncrementalHashStream.ThreadStaticInstance = null;

        foreach (object? value in values)
        {
            JsonSerializer.Serialize(stream, value, serializerOptions.GetTypeInfo(typeof(object)));
        }

        Span<byte> hashData = stackalloc byte[SHA256.HashSizeInBytes];
        stream.GetHashAndReset(hashData);
        IncrementalHashStream.ThreadStaticInstance = stream;

        return Convert.ToHexString(hashData);
#else
        MemoryStream stream = new();
        foreach (object? value in values)
        {
            JsonSerializer.Serialize(stream, value, serializerOptions.GetTypeInfo(typeof(object)));
        }

        using var sha256 = SHA256.Create();
        stream.Position = 0;
        var hashData = sha256.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);

        var chars = new char[hashData.Length * 2];
        int destPos = 0;
        foreach (byte b in hashData)
        {
            int div = Math.DivRem(b, 16, out int rem);
            chars[destPos++] = ToHexChar(div);
            chars[destPos++] = ToHexChar(rem);

            static char ToHexChar(int i) => (char)(i < 10 ? i + '0' : i - 10 + 'A');
        }

        Debug.Assert(destPos == chars.Length, "Expected to have filled the entire array.");

        return new string(chars);
#endif
    }

#if NET
    /// <summary>Provides a stream that writes to an <see cref="IncrementalHash"/>.</summary>
    private sealed class IncrementalHashStream : Stream
    {
        /// <summary>A per-thread instance of <see cref="IncrementalHashStream"/>.</summary>
        /// <remarks>An instance stored must be in a reset state ready to be used by another consumer.</remarks>
        [ThreadStatic]
        public static IncrementalHashStream? ThreadStaticInstance;

        /// <summary>Gets the current hash and resets.</summary>
        public void GetHashAndReset(Span<byte> bytes) => _hash.GetHashAndReset(bytes);

        /// <summary>The <see cref="IncrementalHash"/> used by this instance.</summary>
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        protected override void Dispose(bool disposing)
        {
            _hash.Dispose();
            base.Dispose(disposing);
        }

        public override void WriteByte(byte value) => Write(new ReadOnlySpan<byte>(in value));
        public override void Write(byte[] buffer, int offset, int count) => _hash.AppendData(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _hash.AppendData(buffer);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
#endif
}
