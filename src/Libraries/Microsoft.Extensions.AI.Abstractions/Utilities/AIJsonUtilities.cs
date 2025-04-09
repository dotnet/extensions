// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if !NET
using System.Diagnostics;
#endif
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
#if NET
using System.Threading;
using System.Threading.Tasks;
#endif
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>
    /// Adds a custom content type to the polymorphic configuration for <see cref="AIContent"/>.
    /// </summary>
    /// <typeparam name="TContent">The custom content type to configure.</typeparam>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="typeDiscriminatorId">The type discriminator id for the content type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="typeDiscriminatorId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TContent"/> is a built-in content type.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="options"/> is a read-only instance.</exception>
    public static void AddAIContentType<TContent>(this JsonSerializerOptions options, string typeDiscriminatorId)
        where TContent : AIContent
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(typeDiscriminatorId);

        AddAIContentTypeCore(options, typeof(TContent), typeDiscriminatorId);
    }

    /// <summary>
    /// Adds a custom content type to the polymorphic configuration for <see cref="AIContent"/>.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="contentType">The custom content type to configure.</param>
    /// <param name="typeDiscriminatorId">The type discriminator id for the content type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/>, <paramref name="contentType"/>, or <paramref name="typeDiscriminatorId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="contentType"/> is a built-in content type or does not derived from <see cref="AIContent"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="options"/> is a read-only instance.</exception>
    public static void AddAIContentType(this JsonSerializerOptions options, Type contentType, string typeDiscriminatorId)
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(contentType);
        _ = Throw.IfNull(typeDiscriminatorId);

        if (!typeof(AIContent).IsAssignableFrom(contentType))
        {
            Throw.ArgumentException(nameof(contentType), "The content type must derive from AIContent.");
        }

        AddAIContentTypeCore(options, contentType, typeDiscriminatorId);
    }

    /// <summary>Serializes the supplied values and computes a string hash of the resulting JSON.</summary>
    /// <param name="values">The data to serialize and from which a hash should be computed.</param>
    /// <param name="serializerOptions">
    /// The <see cref="JsonSerializerOptions"/> to use for serializing the values.
    /// If <see langword="null"/>, <see cref="DefaultOptions"/> will be used.
    /// </param>
    /// <returns>A string that will be used as a cache key.</returns>
    /// <remarks>
    /// The resulting hash may be used for purposes like caching. However, while the generated
    /// hash is deterministic for the same inputs, it is not guaranteed to be stable across releases
    /// of the library, as exactly how the hash is computed may change from version to version.
    /// </remarks>
    public static string HashDataToString(ReadOnlySpan<object?> values, JsonSerializerOptions? serializerOptions = null)
    {
        if (serializerOptions is null)
        {
            serializerOptions = DefaultOptions;
        }
        else
        {
            serializerOptions.MakeReadOnly();
        }

        JsonTypeInfo jti = serializerOptions.GetTypeInfo(typeof(object));

        // For cases where the hash may be used as a cache key, we rely on collision resistance for security purposes.
        // If a collision occurs, we'd serve a cached LLM response for a potentially unrelated prompt, leading to information
        // disclosure. Use of SHA384 is an implementation detail and can be easily swapped in the future if needed, albeit
        // invalidating any existing cache entries.
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

        Span<byte> hashData = stackalloc byte[SHA384.HashSizeInBytes];
        try
        {
            foreach (object? value in values)
            {
                JsonSerializer.Serialize(stream, value, jti);
            }

            stream.GetHashAndReset(hashData);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        IncrementalHashStream.ThreadStaticInstance = stream;

        return Convert.ToHexString(hashData);
#else
        MemoryStream stream = new();
        foreach (object? value in values)
        {
            JsonSerializer.Serialize(stream, value, jti);
        }

        using var hashAlgorithm = SHA384.Create();
        var hashData = hashAlgorithm.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);

        return ConvertToHexString(hashData);

        static string ConvertToHexString(ReadOnlySpan<byte> hashData)
        {
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
        }
#endif
    }

    private static void AddAIContentTypeCore(JsonSerializerOptions options, Type contentType, string typeDiscriminatorId)
    {
        if (contentType.Assembly == typeof(AIContent).Assembly)
        {
            Throw.ArgumentException(nameof(contentType), "Cannot register built-in AI content types.");
        }

        IJsonTypeInfoResolver resolver = options.TypeInfoResolver ?? DefaultOptions.TypeInfoResolver!;
        options.TypeInfoResolver = resolver.WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(AIContent))
            {
                (typeInfo.PolymorphismOptions ??= new()).DerivedTypes.Add(new(contentType, typeDiscriminatorId));
            }
        });
    }

#if NET
    /// <summary>Provides a stream that writes to an <see cref="IncrementalHash"/>.</summary>
    private sealed class IncrementalHashStream : Stream
    {
        /// <summary>A per-thread instance of <see cref="IncrementalHashStream"/>.</summary>
        /// <remarks>An instance stored must be in a reset state ready to be used by another consumer.</remarks>
        [ThreadStatic]
        public static IncrementalHashStream? ThreadStaticInstance;

        /// <summary>The <see cref="IncrementalHash"/> used by this instance.</summary>
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA384);

        /// <summary>Gets the current hash and resets.</summary>
        public void GetHashAndReset(Span<byte> bytes) => _hash.GetHashAndReset(bytes);

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

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override bool CanWrite => true;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _hash.Dispose();
            base.Dispose(disposing);
        }
    }
#endif
}
