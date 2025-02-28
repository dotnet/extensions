// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if !NET
using System.Diagnostics;
#endif
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1502 // Element should not be on a single line

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that caches the results of response calls, storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed
/// <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </remarks>
public class DistributedCachingChatClient : CachingChatClient
{
    /// <summary>The <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</summary>
    private readonly IDistributedCache _storage;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use when serializing cache data.</summary>
    private JsonSerializerOptions _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="storage">An <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    public DistributedCachingChatClient(IChatClient innerClient, IDistributedCache storage)
        : base(innerClient)
    {
        _storage = Throw.IfNull(storage);
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc />
    protected override async Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (ChatResponse?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken).ConfigureAwait(false) is byte[] existingJson)
        {
            return (IReadOnlyList<ChatResponseUpdate>?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        await _storage.SetAsync(key, newJson, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="values"/> are serialized to JSON using <see cref="JsonSerializerOptions"/> in order to compute the key.
    /// </para>
    /// <para>
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </para>
    /// </remarks>
    protected override string GetCacheKey(params ReadOnlySpan<object?> values) =>
        GetDefaultCacheKey(values, _jsonSerializerOptions);

    /// <summary>Computes a default cache key for the specified parameters.</summary>
    /// <param name="values">The data with which to compute the key.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing the values.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    /// <remarks>
    /// This is the default implementation used by a <see cref="DistributedCachingChatClient"/> in its
    /// <see cref="GetCacheKey"/> unless overridden by a derived type.
    /// JSON serialization is used to serialize the values as part of producing the cache key.
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </remarks>
    public static string GetDefaultCacheKey(ReadOnlySpan<object?> values, JsonSerializerOptions? serializerOptions = null)
    {
        if (serializerOptions is null)
        {
            serializerOptions = AIJsonUtilities.DefaultOptions;
        }
        else
        {
            serializerOptions.MakeReadOnly();
        }

        // The complete JSON representation is excessively long for a cache key, duplicating much of the content
        // from the value. So we use a hash of it as the default key, and we rely on collision resistance for security purposes.
        // If a collision occurs, we'd serve the cached LLM response for a potentially unrelated prompt, leading to information
        // disclosure. Use of SHA256 is an implementation detail and can be easily swapped in the future if needed, albeit
        // invalidating any existing cache entries that may exist in whatever IDistributedCache was in use.

#if NET
        IncrementalHashStream? stream = IncrementalHashStream.ThreadStaticInstance;
        if (stream is not null)
        {
            // We need to ensure that the value in ThreadStaticInstance is always ready to use.
            // If we start using an instance, write to it, and then fail, we will have left it
            // in an inconsistent state. So, when renting it, we null it out, and we only put
            // it back upon successful completion after resetting it.
            IncrementalHashStream.ThreadStaticInstance = null;
        }
        else
        {
            stream = new();
        }

        string result;
        try
        {
            foreach (object? value in values)
            {
                JsonSerializer.Serialize(stream, value, serializerOptions.GetTypeInfo(typeof(object)));
            }

            Span<byte> hashData = stackalloc byte[SHA256.HashSizeInBytes];
            stream.GetHashAndReset(hashData);

            result = Convert.ToHexString(hashData);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        IncrementalHashStream.ThreadStaticInstance = stream;
        return result;
#else
        MemoryStream stream = new();
        foreach (object? value in values)
        {
            JsonSerializer.Serialize(stream, value, serializerOptions.GetTypeInfo(typeof(object)));
        }

        using var sha256 = SHA256.Create();
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
