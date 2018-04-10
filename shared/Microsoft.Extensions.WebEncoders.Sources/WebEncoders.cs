// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.WebEncoders.Sources;

#if !NET461
using System.Numerics;
#endif

#if WebEncoders_In_WebUtilities
namespace Microsoft.AspNetCore.WebUtilities
#else
namespace Microsoft.Extensions.Internal
#endif
{
    /// <summary>
    /// Contains utility APIs to assist with common encoding and decoding operations.
    /// </summary>
#if WebEncoders_In_WebUtilities
    public
#else
    internal
#endif
    static class WebEncoders
    {
        private const int MaxStackallocBytes = 256;
        private const int MaxEncodedLength = (int.MaxValue / 4) * 3;  // encode inflates the data by 4/3
        private static readonly byte[] EmptyBytes = new byte[0];

        /// <summary>
        /// Decodes a base64url-encoded string.
        /// </summary>
        /// <param name="input">The base64url-encoded input to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input)
        {
            if (input == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Base64UrlDecode(input.AsSpan());
        }

        /// <summary>
        /// Decodes a base64url-encoded substring of a given string.
        /// </summary>
        /// <param name="input">A string containing the base64url-encoded input to decode.</param>
        /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
        /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input, int offset, int count)
        {
            if (input == null
                || (uint)offset > (uint)input.Length
                || (uint)count > (uint)(input.Length - offset))
            {
                ThrowInvalidArguments(input, offset, count);
            }

            return Base64UrlDecode(input.AsSpan(offset, count));
        }

        /// <summary>
        /// Decodes a base64url-encoded span of chars.
        /// </summary>
        /// <param name="base64Url">The base64url-encoded input to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(ReadOnlySpan<char> base64Url)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                return EmptyBytes;
            }

            var base64Len = GetBufferSizeRequiredToUrlDecode(base64Url.Length, out int dataLength);
            var data = new byte[dataLength];
            var written = Base64UrlDecodeCore(base64Url, data, base64Len);
            Debug.Assert(data.Length == written);

            return data;
        }

        /// <summary>
        /// Decodes a base64url-encoded span of chars into a span of bytes.
        /// </summary>
        /// <param name="base64Url">A span containing the base64url-encoded input to decode.</param>
        /// <param name="data">The base64url-decoded form of <paramref name="base64Url"/>.</param>
        /// <returns>The number of the bytes written to <paramref name="data"/>.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static int Base64UrlDecode(ReadOnlySpan<char> base64Url, Span<byte> data)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                return 0;
            }

            var base64Len = GetBufferSizeRequiredToUrlDecode(base64Url.Length, out int dataLength);
            var written = Base64UrlDecodeCore(base64Url, data, base64Len);
            Debug.Assert(dataLength == written);

            return written;
        }

        /// <summary>
        /// Decode the span of UTF-8 base64url-encoded text into binary data.
        /// </summary>
        /// <param name="base64Url">The input span which contains UTF-8 base64url-encoded text that needs to be decoded.</param>
        /// <param name="data">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock">True (default) when the input span contains the entire data to decode.
        /// Set to false only if it is known that the input span contains partial data with more data to follow.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if isFinalBlock is false and the input is not a multiple of 4, otherwise the partial input would be considered as InvalidData
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4) and isFinalBlock is true.</returns>
        public static OperationStatus Base64UrlDecode(ReadOnlySpan<byte> base64Url, Span<byte> data, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            var base64Len = isFinalBlock
                ? GetBufferSizeRequiredToUrlDecode(base64Url.Length, out int dataLength)
                : base64Url.Length;

            if (base64Len > MaxStackallocBytes / sizeof(byte))
            {
                return Base64UrlDecodeCoreSlow(base64Url, data, base64Len, out bytesConsumed, out bytesWritten, isFinalBlock);
            }

            Span<byte> base64 = stackalloc byte[base64Len];
            return Base64UrlDecodeCore(base64Url, base64, data, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Decodes a base64url-encoded <paramref name="input"/> into a <c>byte[]</c>.
        /// </summary>
        /// <param name="input">A string containing the base64url-encoded input to decode.</param>
        /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
        /// <param name="buffer">
        /// Scratch buffer to hold the <see cref="char"/>s to decode. Array must be large enough to hold
        /// <paramref name="bufferOffset"/> and <paramref name="count"/> characters as well as Base64 padding
        /// characters. Content is not preserved.
        /// </param>
        /// <param name="bufferOffset">
        /// The offset into <paramref name="buffer"/> at which to begin writing the <see cref="char"/>s to decode.
        /// </param>
        /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
        /// <returns>The base64url-decoded form of the <paramref name="input"/>.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input, int offset, char[] buffer, int bufferOffset, int count)
        {
            if (input == null
                || (uint)offset > (uint)input.Length
                || (uint)count > (uint)(input.Length - offset)
                || buffer == null
                || (uint)bufferOffset > (uint)buffer.Length)
            {
                ThrowInvalidArguments(input, offset, count, buffer, bufferOffset, validateBuffer: true);
            }

            // Special-case empty input
            if (count == 0)
            {
                return EmptyBytes;
            }

            var base64Len = GetBufferSizeRequiredToUrlDecode(count, out int dataLength);

            if ((uint)buffer.Length < (uint)(bufferOffset + base64Len))
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.bufferOffset, ExceptionArgument.input);
            }

#if NETCOREAPP2_1
            var data = new byte[dataLength];
            var base64 = buffer.AsSpan(bufferOffset, base64Len);
            UrlCharsHelper.SubstituteUrlCharsForDecoding(input.AsSpan(offset, count), base64);
            Convert.TryFromBase64Chars(base64, data, out int written);
            Debug.Assert(written == dataLength);

            return data;
#else
            UrlCharsHelper.SubstituteUrlCharsForDecoding(input.AsSpan(offset, count), buffer.AsSpan(bufferOffset, base64Len));
            return Convert.FromBase64CharArray(buffer, bufferOffset, base64Len);
#endif
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url-encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(byte[] input)
        {
            if (input == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Base64UrlEncode(input.AsSpan());
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url-encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
        /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(byte[] input, int offset, int count)
        {
            if (input == null
                || (uint)offset > (uint)input.Length
                || (uint)count > (uint)(input.Length - offset))
            {
                ThrowInvalidArguments(input, offset, count);
            }

            return Base64UrlEncode(input.AsSpan(offset, count));
        }

        /// <summary>
        /// Encodes <paramref name="data"/> using base64url-encoding.
        /// </summary>
        /// <param name="data">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="data"/>.</returns>
        public static unsafe string Base64UrlEncode(ReadOnlySpan<byte> data)
        {
            // Special-case empty input
            if (data.IsEmpty)
            {
                return string.Empty;
            }

            var base64Len = GetBufferSizeRequiredToBase64Encode(data.Length, out int numPaddingChars);
#if NETCOREAPP2_1
            fixed (byte* ptr = &MemoryMarshal.GetReference(data))
            {
                return string.Create(base64Len - numPaddingChars, (Ptr: (IntPtr)ptr, data.Length, base64Len), (base64Url, state) =>
                {
                    var bytes = new ReadOnlySpan<byte>(state.Ptr.ToPointer(), state.Length);

                    Base64UrlEncodeCore(bytes, base64Url, state.base64Len);
                });
            }
#else
#if !NET461
            char[] arrayToReturnToPool = null;
            try
            {
#endif
                var base64UrlLen = base64Len - numPaddingChars;
                var base64Url = base64UrlLen <= MaxStackallocBytes / sizeof(char)
                    ? stackalloc char[base64UrlLen]
#if NET461
                    : new char[base64UrlLen];
#else
                    : arrayToReturnToPool = ArrayPool<char>.Shared.Rent(base64UrlLen);
#endif
                var urlEncodedLen = Base64UrlEncodeCore(data, base64Url, base64Len);
                Debug.Assert(base64UrlLen == urlEncodedLen);

                fixed (char* ptr = &MemoryMarshal.GetReference(base64Url))
                {
                    return new string(ptr, 0, urlEncodedLen);
                }
#if !NET461
            }
            finally
            {
                if (arrayToReturnToPool != null)
                {
                    ArrayPool<char>.Shared.Return(arrayToReturnToPool);
                }
            }
#endif
#endif
        }

        /// <summary>
        /// Encodes <paramref name="data"/> using base64url-encoding into <paramref name="base64Url"/>.
        /// </summary>
        /// <param name="data">The binary input to encode.</param>
        /// <param name="base64Url">The base64url-encoded form of <paramref name="data"/>.</param>
        /// <returns>The number of chars written to <paramref name="base64Url"/>.</returns>
        public static int Base64UrlEncode(ReadOnlySpan<byte> data, Span<char> base64Url)
        {
            // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

            // Special-case empty input
            if (data.IsEmpty)
            {
                return 0;
            }

            var base64Len = GetArraySizeRequiredToEncode(data.Length);
            return Base64UrlEncodeCore(data, base64Url, base64Len);
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 base64url-encoded representation.
        /// </summary>
        /// <param name="data">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="base64Url">
        /// The output span which contains the result of the operation, i.e. the UTF-8 base64url-encoded text.
        /// The span must be large enough to hold the full base64-encoded form of <paramref name="data"/>, included padding characters.
        /// </param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock">True (default) when the input span contains the entire data to decode.
        /// Set to false only if it is known that the input span contains partial data with more data to follow.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if isFinalBlock is false, otherwise the output is padded if the input is not a multiple of 3
        /// It does not return InvalidData since that is not possible for base 64 encoding.</returns>
        public static OperationStatus Base64UrlEncode(ReadOnlySpan<byte> data, Span<byte> base64Url, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            // Special-case empty input
            if (data.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            var status = Base64.EncodeToUtf8(data, base64Url, out bytesConsumed, out bytesWritten, isFinalBlock);

            if (status == OperationStatus.Done || status == OperationStatus.NeedMoreData)
            {
                bytesWritten = UrlCharsHelper.SubstituteUrlCharsForEncoding(base64Url, bytesWritten);
            }

            return status;
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url-encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
        /// <param name="output">
        /// Buffer to receive the base64url-encoded form of <paramref name="input"/>. Array must be large enough to
        /// hold <paramref name="outputOffset"/> characters and the full base64-encoded form of
        /// <paramref name="input"/>, including padding characters.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into <paramref name="output"/> at which to begin writing the base64url-encoded form of
        /// <paramref name="input"/>.
        /// </param>
        /// <param name="count">The number of <c>byte</c>s from <paramref name="input"/> to encode.</param>
        /// <returns>
        /// The number of characters written to <paramref name="output"/>, less any padding characters.
        /// </returns>
        public static int Base64UrlEncode(byte[] input, int offset, char[] output, int outputOffset, int count)
        {
            if (input == null
                || (uint)offset > (uint)input.Length
                || (uint)count > (uint)(input.Length - offset)
                || output == null
                || (uint)outputOffset > (uint)output.Length)
            {
                ThrowInvalidArguments(input, offset, count, output, outputOffset, ExceptionArgument.output, validateBuffer: true);
            }

            var base64Len = GetArraySizeRequiredToEncode(count);
            if ((uint)output.Length < (uint)(outputOffset + base64Len))
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.outputOffset, ExceptionArgument.output);
            }

            // Special-case empty input.
            if (count == 0)
            {
                return 0;
            }

            return Base64UrlEncodeCore(input.AsSpan(offset, count), output.AsSpan(outputOffset), base64Len);
        }

        /// <summary>
        /// Gets the minimum buffer size required for decoding of <paramref name="count"/> characters.
        /// </summary>
        /// <param name="count">The number of characters to decode.</param>
        /// <returns>
        /// The minimum buffer size required for decoding  of <paramref name="count"/> characters.
        /// </returns>
        /// <remarks>
        /// The returned buffer size is large enough to hold <paramref name="count"/> characters as well
        /// as base64 padding characters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetArraySizeRequiredToDecode(int count)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            return count == 0 ? 0 : GetBufferSizeRequiredToUrlDecode(count, out int dataLength);
        }

        /// <summary>
        /// Gets the minimum output buffer size required for encoding <paramref name="count"/> bytes.
        /// </summary>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>
        /// The minimum output buffer size required for encoding <paramref name="count"/> <see cref="byte"/>s.
        /// </returns>
        /// <remarks>
        /// The returned buffer size is large enough to hold <paramref name="count"/> bytes as well
        /// as base64 padding characters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetArraySizeRequiredToEncode(int count)
        {
            if ((uint)count > MaxEncodedLength)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            return count == 0 ? 0 : GetBufferSizeRequiredToBase64Encode(count);
        }

        private static int Base64UrlDecodeCore(ReadOnlySpan<char> base64Url, Span<byte> data, int base64Len)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");

            if (base64Len > MaxStackallocBytes / sizeof(char))
            {
                return Base64UrlDecodeCoreSlow(base64Url, data, base64Len);
            }

            Span<byte> base64Bytes = stackalloc byte[base64Len];
            return Base64UrlDecodeCore(base64Url, base64Bytes, data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Base64UrlDecodeCoreSlow(ReadOnlySpan<char> base64Url, Span<byte> data, int base64Len)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");
#if !NET461
            byte[] arrayToReturnToPool = null;
            try
            {
                var base64Bytes = new Span<byte>(arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(base64Len), 0, base64Len);
                return Base64UrlDecodeCore(base64Url, base64Bytes, data);
            }
            finally
            {
                if (arrayToReturnToPool != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }
            }
#else
            var base64Bytes = new byte[base64Len];
            return Base64UrlDecodeCore(base64Url, base64Bytes, data);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Base64UrlDecodeCore(ReadOnlySpan<char> base64Url, Span<byte> base64Bytes, Span<byte> data)
        {
            UrlCharsHelper.SubstituteUrlCharsForDecoding(base64Url, base64Bytes);
            var status = Base64.DecodeFromUtf8(base64Bytes, data, out int consumed, out int written);

            if (status != OperationStatus.Done)
            {
                ThrowHelper.ThrowOperationNotDone(status);
            }

            return written;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static OperationStatus Base64UrlDecodeCoreSlow(ReadOnlySpan<byte> base64Url, Span<byte> data, int base64Len, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");
#if NET461
            var base64 = new byte[base64Len];
            return Base64UrlDecodeCore(base64Url, base64, data, out bytesConsumed, out bytesWritten, isFinalBlock);
#else
            byte[] arrayToReturnToPool = null;
            try
            {
                var base64 = new Span<byte>(arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(base64Len), 0, base64Len);
                return Base64UrlDecodeCore(base64Url, base64, data, out bytesConsumed, out bytesWritten, isFinalBlock);
            }
            finally
            {
                if (arrayToReturnToPool != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationStatus Base64UrlDecodeCore(ReadOnlySpan<byte> base64Url, Span<byte> base64, Span<byte> data, out int consumed, out int written, bool isFinalBlock)
        {
            UrlCharsHelper.SubstituteUrlCharsForDecoding(base64Url, base64, isFinalBlock);
            var status = Base64.DecodeFromUtf8(base64, data, out consumed, out written, isFinalBlock);

            if (status != OperationStatus.Done && status != OperationStatus.NeedMoreData)
            {
                ThrowHelper.ThrowOperationNotDone(status);
            }

            // Fix bytesConsumed to match the input 'base64Url' (and not the 'base64')
            consumed = base64Url.Length - (base64.Length - consumed);

            return status;
        }

#if NETCOREAPP2_1
        private static int Base64UrlEncodeCore(ReadOnlySpan<byte> data, Span<char> base64Url, int base64Len)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");

            if (base64Len > MaxStackallocBytes / sizeof(char))
            {
                return Base64UrlEncodeCoreSlow(data, base64Url, base64Len);
            }

            Span<char> base64 = stackalloc char[base64Len];
            Convert.TryToBase64Chars(data, base64, out int written);
            return UrlCharsHelper.SubstituteUrlCharsForEncoding(base64, base64Url);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Base64UrlEncodeCoreSlow(ReadOnlySpan<byte> data, Span<char> base64Url, int base64Len)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");

            char[] arrayToReturnToPool = null;
            try
            {
                var base64 = new Span<char>(arrayToReturnToPool = ArrayPool<char>.Shared.Rent(base64Len), 0, base64Len);
                Convert.TryToBase64Chars(data, base64, out int written);
                return UrlCharsHelper.SubstituteUrlCharsForEncoding(base64, base64Url);
            }
            finally
            {
                if (arrayToReturnToPool != null)
                {
                    ArrayPool<char>.Shared.Return(arrayToReturnToPool);
                }
            }
        }
#else
        private static int Base64UrlEncodeCore(ReadOnlySpan<byte> data, Span<char> base64Url, int base64Len)
        {
            // Internal usage, so it can be assumed that base64Len is exactely a multiple of 4
            Debug.Assert(base64Len % 4 == 0, "base64Len must be multiple of 4");
#if !NET461
            byte[] arrayToReturnToPool = null;
            try
            {
#endif
                var base64Bytes = base64Len <= MaxStackallocBytes / sizeof(byte)
                    ? stackalloc byte[base64Len]
#if NET461
                    : new byte[base64Len];
#else
                    : new Span<byte>(arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(base64Len), 0, base64Len);
#endif
                var status = Base64.EncodeToUtf8(data, base64Bytes, out int consumed, out int written);

                if (status != OperationStatus.Done)
                {
                    ThrowHelper.ThrowOperationNotDone(status);
                }

                return UrlCharsHelper.SubstituteUrlCharsForEncoding(base64Bytes, base64Url);
#if !NET461
            }
            finally
            {
                if (arrayToReturnToPool != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }
            }
#endif
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBufferSizeRequiredToUrlDecode(int urlEncodedLen, out int dataLength, bool isFinalBlock = true)
        {
            if (isFinalBlock)
            {
                // Shortcut for Guid and other 16 byte data
                if (urlEncodedLen == 22)
                {
                    dataLength = 16;
                    return 24;
                }

                var numPaddingChars = GetNumBase64PaddingCharsToAddForDecode(urlEncodedLen);
                var base64Len = urlEncodedLen + numPaddingChars;
                Debug.Assert(base64Len % 4 == 0, "Invariant: Array length must be a multiple of 4.");
                dataLength = (base64Len >> 2) * 3 - numPaddingChars;

                return base64Len;
            }
            else
            {
                dataLength = (urlEncodedLen >> 2) * 3;
                return urlEncodedLen;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumBase64PaddingCharsToAddForDecode(int urlEncodedLen)
        {
            // Calculation is:
            // switch (inputLength % 4)
            // 0 -> 0
            // 2 -> 2
            // 3 -> 1
            // default -> format exception

            var result = (4 - urlEncodedLen) & 3;

            if (result == 3)
            {
                ThrowHelper.ThrowMalformedInputException(urlEncodedLen);
            }

            return result;
        }

        private static int GetBufferSizeRequiredToBase64Encode(int dataLength)
        {
            // overflow conditions are already eliminated, so 'checked' is not necessary
            Debug.Assert(dataLength >= 0 && dataLength <= MaxEncodedLength);

            var numWholeOrPartialInputBlocks = (dataLength + 2) / 3;
            return numWholeOrPartialInputBlocks * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBufferSizeRequiredToBase64Encode(int dataLength, out int numPaddingChars)
        {
            // Shortcut for Guid and other 16 byte data
            if (dataLength == 16)
            {
                numPaddingChars = 2;
                return 24;
            }

            numPaddingChars = GetNumBase64PaddingCharsAddedByEncode(dataLength);
            return GetBufferSizeRequiredToBase64Encode(dataLength);
        }

        private static int GetNumBase64PaddingCharsAddedByEncode(int dataLength)
        {
            // Calculation is:
            // switch (dataLength % 3)
            // 0 -> 0
            // 1 -> 2
            // 2 -> 1

            return dataLength % 3 == 0 ? 0 : 3 - dataLength % 3;
        }

        private static void ThrowInvalidArguments(object input, int offset, int count, char[] buffer = null, int bufferOffset = 0, ExceptionArgument bufferName = ExceptionArgument.buffer, bool validateBuffer = false)
        {
            throw GetInvalidArgumentsException();

            Exception GetInvalidArgumentsException()
            {
                if (input == null)
                {
                    return ThrowHelper.GetArgumentNullException(ExceptionArgument.input);
                }

                if (validateBuffer && buffer == null)
                {
                    return ThrowHelper.GetArgumentNullException(bufferName);
                }

                if (offset < 0)
                {
                    return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.offset);
                }

                if (count < 0)
                {
                    return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.count);
                }

                if (bufferOffset < 0)
                {
                    return ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.bufferOffset);
                }

                return ThrowHelper.GetInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.offset, ExceptionArgument.input);
            }
        }

        internal static class UrlEncoder<T>
        {
            public static OperationStatus Decode(ReadOnlySpan<T> urlEncoded, Span<byte> data, out int consumed, out int written, bool isFinalBlock = true)
            {
                ref T source = ref MemoryMarshal.GetReference(urlEncoded);
                ref byte destBytes = ref MemoryMarshal.GetReference(data);

                int base64Len = GetBufferSizeRequiredToUrlDecode(urlEncoded.Length, out int dataLength, isFinalBlock);
                int srcLength = base64Len & ~0x3;       // only decode input up to closest multiple of 4.
                int destLength = data.Length;

                int sourceIndex = 0;
                int destIndex = 0;

                if (urlEncoded.Length == 0)
                {
                    goto DoneExit;
                }

                ref sbyte decodingMap = ref s_decodingMap[0];

                // Last bytes could have padding characters, so process them separately and treat them as valid only if isFinalBlock is true.
                // If isFinalBlock is false, padding characters are considered invalid.
                int skipLastChunk = isFinalBlock ? 4 : 0;

                int maxSrcLength = 0;
                if (destLength >= dataLength)
                {
                    maxSrcLength = srcLength - skipLastChunk;
                }
                else
                {
                    // This should never overflow since destLength here is less than int.MaxValue / 4 * 3.
                    // Therefore, (destLength / 3) * 4 will always be less than int.MaxValue.
                    maxSrcLength = (destLength / 3) * 4;
                }

                while (sourceIndex < maxSrcLength)
                {
                    int result = DecodeFour(ref Unsafe.Add(ref source, sourceIndex), ref decodingMap);

                    if (result < 0) goto InvalidExit;

                    WriteThreeLowOrderBytes(ref destBytes, destIndex, result);
                    destIndex += 3;
                    sourceIndex += 4;
                }

                if (maxSrcLength != srcLength - skipLastChunk)
                {
                    goto DestinationSmallExit;
                }

                // If input is less than 4 bytes, srcLength == sourceIndex == 0
                // If input is not a multiple of 4, sourceIndex == srcLength != 0
                if (sourceIndex == srcLength)
                {
                    if (isFinalBlock)
                    {
                        goto InvalidExit;
                    }

                    goto NeedMoreExit;
                }

                // If isFinalBlock is false, we will never reach this point.

                // Handle last four bytes. There are 0, 1, 2 padding chars.
                int numPaddingChars = base64Len - urlEncoded.Length;
                ref T lastFourStart = ref Unsafe.Add(ref source, srcLength - 4);

                if (numPaddingChars == 0)
                {
                    int result = DecodeFour(ref lastFourStart, ref decodingMap);

                    if (result < 0) goto InvalidExit;
                    if (destIndex > destLength - 3) goto DestinationSmallExit;

                    WriteThreeLowOrderBytes(ref destBytes, destIndex, result);
                    destIndex += 3;
                    sourceIndex += 4;
                }
                else if (numPaddingChars == 1)
                {
                    int result = DecodeThree(ref lastFourStart, ref decodingMap);

                    if (result < 0) goto InvalidExit;
                    if (destIndex > destLength - 2) goto DestinationSmallExit;

                    WriteTwoLowOrderBytes(ref destBytes, destIndex, result);
                    destIndex += 2;
                    sourceIndex += 3;
                }
                else
                {
                    int result = DecodeTwo(ref lastFourStart, ref decodingMap);

                    if (result < 0) goto InvalidExit;
                    if (destIndex > destLength - 1) goto DestinationSmallExit;

                    WriteOneLowOrderByte(ref destBytes, destIndex, result);
                    destIndex += 1;
                    sourceIndex += 2;
                }

                if (srcLength != base64Len)
                {
                    goto InvalidExit;
                }

            DoneExit:
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.Done;

            DestinationSmallExit:
                if (srcLength != urlEncoded.Length && isFinalBlock)
                {
                    goto InvalidExit;   // if input is not a multiple of 4, and there is no more data, return invalid data instead
                }
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.DestinationTooSmall;

            NeedMoreExit:
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.NeedMoreData;

            InvalidExit:
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.InvalidData;
            }

            public static OperationStatus Encode(ReadOnlySpan<byte> data, Span<T> urlEncoded, out int consumed, out int written, bool isFinalBlock = true)
            {
                ref byte srcBytes = ref MemoryMarshal.GetReference(data);
                ref T destination = ref MemoryMarshal.GetReference(urlEncoded);

                int srcLength = data.Length;
                int destLength = urlEncoded.Length;

                int maxSrcLength = -2;
                if (srcLength <= MaxEncodedLength && destLength >= GetBufferSizeRequiredToBase64Encode(srcLength, out int numPaddingChars) - numPaddingChars)
                {
                    maxSrcLength += srcLength;
                }
                else
                {
                    maxSrcLength += (destLength >> 2) * 3;
                }

                int sourceIndex = 0;
                int destIndex = 0;

                ref byte encodingMap = ref s_encodingMap[0];

                while (sourceIndex < maxSrcLength)
                {
                    EncodeThreeBytes(ref Unsafe.Add(ref srcBytes, sourceIndex), ref Unsafe.Add(ref destination, destIndex), ref encodingMap);
                    destIndex += 4;
                    sourceIndex += 3;
                }

                if (maxSrcLength != srcLength - 2)
                    goto DestinationSmallExit;

                if (!isFinalBlock)
                    goto NeedMoreDataExit;

                if (sourceIndex == srcLength - 1)
                {
                    EncodeOneByte(ref Unsafe.Add(ref srcBytes, sourceIndex), ref Unsafe.Add(ref destination, destIndex), ref encodingMap);
                    destIndex += 2;
                    sourceIndex += 1;
                }
                else if (sourceIndex == srcLength - 2)
                {
                    EncodeTwoBytes(ref Unsafe.Add(ref srcBytes, sourceIndex), ref Unsafe.Add(ref destination, destIndex), ref encodingMap);
                    destIndex += 3;
                    sourceIndex += 2;
                }

                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.Done;

            NeedMoreDataExit:
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.NeedMoreData;

            DestinationSmallExit:
                consumed = sourceIndex;
                written = destIndex;
                return OperationStatus.DestinationTooSmall;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int DecodeFour(ref T encoded, ref sbyte decodingMap)
            {
                int i0, i1, i2, i3;

                if (typeof(T) == typeof(byte))
                {
                    ref byte tmp = ref Unsafe.As<T, byte>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                    i2 = Unsafe.Add(ref tmp, 2);
                    i3 = Unsafe.Add(ref tmp, 3);
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char tmp = ref Unsafe.As<T, char>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                    i2 = Unsafe.Add(ref tmp, 2);
                    i3 = Unsafe.Add(ref tmp, 3);
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                i0 = Unsafe.Add(ref decodingMap, i0);
                i1 = Unsafe.Add(ref decodingMap, i1);
                i2 = Unsafe.Add(ref decodingMap, i2);
                i3 = Unsafe.Add(ref decodingMap, i3);

                return i0 << 18
                    | i1 << 12
                    | i2 << 6
                    | i3;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int DecodeThree(ref T encoded, ref sbyte decodingMap)
            {
                int i0, i1, i2;

                if (typeof(T) == typeof(byte))
                {
                    ref byte tmp = ref Unsafe.As<T, byte>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                    i2 = Unsafe.Add(ref tmp, 2);
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char tmp = ref Unsafe.As<T, char>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                    i2 = Unsafe.Add(ref tmp, 2);
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                i0 = Unsafe.Add(ref decodingMap, i0);
                i1 = Unsafe.Add(ref decodingMap, i1);
                i2 = Unsafe.Add(ref decodingMap, i2);

                return i0 << 18
                    | i1 << 12
                    | i2 << 6;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int DecodeTwo(ref T encoded, ref sbyte decodingMap)
            {
                int i0, i1;

                if (typeof(T) == typeof(byte))
                {
                    ref byte tmp = ref Unsafe.As<T, byte>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char tmp = ref Unsafe.As<T, char>(ref encoded);
                    i0 = Unsafe.Add(ref tmp, 0);
                    i1 = Unsafe.Add(ref tmp, 1);
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                i0 = Unsafe.Add(ref decodingMap, i0);
                i1 = Unsafe.Add(ref decodingMap, i1);

                return i0 << 18
                    | i1 << 12;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteThreeLowOrderBytes(ref byte destination, int destIndex, int value)
            {
                Unsafe.Add(ref destination, destIndex + 0) = (byte)(value >> 16);
                Unsafe.Add(ref destination, destIndex + 1) = (byte)(value >> 8);
                Unsafe.Add(ref destination, destIndex + 2) = (byte)value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteTwoLowOrderBytes(ref byte destination, int destIndex, int value)
            {
                Unsafe.Add(ref destination, destIndex + 0) = (byte)(value >> 16);
                Unsafe.Add(ref destination, destIndex + 1) = (byte)(value >> 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteOneLowOrderByte(ref byte destination, int destIndex, int value)
            {
                Unsafe.Add(ref destination, destIndex) = (byte)(value >> 16);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EncodeThreeBytes(ref byte threeBytes, ref T encoded, ref byte encodingMap)
            {
                int i = (threeBytes << 16) | (Unsafe.Add(ref threeBytes, 1) << 8) | Unsafe.Add(ref threeBytes, 2);

                int i0 = Unsafe.Add(ref encodingMap, i >> 18);
                int i1 = Unsafe.Add(ref encodingMap, (i >> 12) & 0x3F);
                int i2 = Unsafe.Add(ref encodingMap, (i >> 6) & 0x3F);
                int i3 = Unsafe.Add(ref encodingMap, i & 0x3F);

                if (typeof(T) == typeof(byte))
                {
                    i = i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
                    Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref encoded), i);
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char enc = ref Unsafe.As<T, char>(ref encoded);
                    Unsafe.Add(ref enc, 0) = (char)i0;
                    Unsafe.Add(ref enc, 1) = (char)i1;
                    Unsafe.Add(ref enc, 2) = (char)i2;
                    Unsafe.Add(ref enc, 3) = (char)i3;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EncodeTwoBytes(ref byte twoBytes, ref T encoded, ref byte encodingMap)
            {
                int i = (twoBytes << 16) | (Unsafe.Add(ref twoBytes, 1) << 8);

                int i0 = Unsafe.Add(ref encodingMap, i >> 18);
                int i1 = Unsafe.Add(ref encodingMap, (i >> 12) & 0x3F);
                int i2 = Unsafe.Add(ref encodingMap, (i >> 6) & 0x3F);

                if (typeof(T) == typeof(byte))
                {
                    ref byte enc = ref Unsafe.As<T, byte>(ref encoded);
                    Unsafe.Add(ref enc, 0) = (byte)i0;
                    Unsafe.Add(ref enc, 1) = (byte)i1;
                    Unsafe.Add(ref enc, 2) = (byte)i2;
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char enc = ref Unsafe.As<T, char>(ref encoded);
                    Unsafe.Add(ref enc, 0) = (char)i0;
                    Unsafe.Add(ref enc, 1) = (char)i1;
                    Unsafe.Add(ref enc, 2) = (char)i2;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EncodeOneByte(ref byte oneByte, ref T encoded, ref byte encodingMap)
            {
                int i = (oneByte << 16);

                int i0 = Unsafe.Add(ref encodingMap, i >> 18);
                int i1 = Unsafe.Add(ref encodingMap, (i >> 12) & 0x3F);

                if (typeof(T) == typeof(byte))
                {
                    ref byte enc = ref Unsafe.As<T, byte>(ref encoded);
                    Unsafe.Add(ref enc, 0) = (byte)i0;
                    Unsafe.Add(ref enc, 1) = (byte)i1;
                }
                else if (typeof(T) == typeof(char))
                {
                    ref char enc = ref Unsafe.As<T, char>(ref encoded);
                    Unsafe.Add(ref enc, 0) = (char)i0;
                    Unsafe.Add(ref enc, 1) = (char)i1;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }
            }

            private static readonly sbyte[] s_decodingMap = {
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1,
                52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,
                -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
                15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, 63,
                -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
            };

            private static readonly byte[] s_encodingMap = {
                 65/*A*/,  66/*B*/,  67/*C*/,  68/*D*/,  69/*E*/,  70/*F*/,  71/*G*/,  72/*H*/,
                 73/*I*/,  74/*J*/,  75/*K*/,  76/*L*/,  77/*M*/,  78/*N*/,  79/*O*/,  80/*P*/,
                 81/*Q*/,  82/*R*/,  83/*S*/,  84/*T*/,  85/*U*/,  86/*V*/,  87/*W*/,  88/*X*/,
                 89/*Y*/,  90/*Z*/,  97/*a*/,  98/*b*/,  99/*c*/, 100/*d*/, 101/*e*/, 102/*f*/,
                103/*g*/, 104/*h*/, 105/*i*/, 106/*j*/, 107/*k*/, 108/*l*/, 109/*m*/, 110/*n*/,
                111/*o*/, 112/*p*/, 113/*q*/, 114/*r*/, 115/*s*/, 116/*t*/, 117/*u*/, 118/*v*/,
                119/*w*/, 120/*x*/, 121/*y*/, 122/*z*/,  48/*0*/,  49/*1*/,  50/*2*/,  51/*3*/,
                 52/*4*/,  53/*5*/,  54/*6*/,  55/*7*/,  56/*8*/,  57/*9*/,  45/*-*/,  95/*_*/
            };
        }

        // TODO: replace IntPtr and (int*) with nuint once available
        internal static unsafe class UrlCharsHelper
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SubstituteUrlCharsForDecoding(ReadOnlySpan<byte> urlEncoded, Span<byte> base64, bool isFinalBlock = true)
            {
                ref var input = ref MemoryMarshal.GetReference(urlEncoded);
                ref var output = ref MemoryMarshal.GetReference(base64);

                SubstituteUrlCharsForDecoding(ref input, ref output, urlEncoded.Length, base64.Length, isFinalBlock);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SubstituteUrlCharsForDecoding(ReadOnlySpan<char> urlEncoded, Span<char> base64)
            {
                ref var input = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(urlEncoded));
                ref var output = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(base64));

                SubstituteUrlCharsForDecoding(ref input, ref output, urlEncoded.Length, base64.Length);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SubstituteUrlCharsForDecoding<T>(ref T urlEncoded, ref T base64, int urlEncodedLen, int base64Len, bool isFinalBlock = true) where T : struct
            {
                // Copy input into base64, fixing up '-' -> '+' and '_' -> '/' and add padding.

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)urlEncodedLen;
                var m = i;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<T>.Count)
                {
                    m = (IntPtr)((int)(int*)n & ~(Vector<T>.Count - 1));
                    for (; (int*)i < (int*)m; i += Vector<T>.Count)
                    {
                        var vec = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref urlEncoded, i)));

                        if (typeof(T) == typeof(byte))
                        {
                            vec = Substitute(vec, (T)(object)(byte)'-', (T)(object)(byte)'+');
                            vec = Substitute(vec, (T)(object)(byte)'_', (T)(object)(byte)'/');
                        }
                        else if (typeof(T) == typeof(ushort))
                        {
                            vec = Substitute(vec, (T)(object)(ushort)'-', (T)(object)(ushort)'+');
                            vec = Substitute(vec, (T)(object)(ushort)'_', (T)(object)(ushort)'/');
                        }
                        else
                        {
                            throw new NotSupportedException();  // just in case new types are introduced in the future
                        }

                        Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref base64, i)), vec);
                    }
                }
#endif
                m = (IntPtr)((int)(int*)n & ~3);
                for (; (int*)i < (int*)m; i += 4)
                {
                    SubstituteUrlCharsForDecoding(ref urlEncoded, ref base64, i + 0);
                    SubstituteUrlCharsForDecoding(ref urlEncoded, ref base64, i + 1);
                    SubstituteUrlCharsForDecoding(ref urlEncoded, ref base64, i + 2);
                    SubstituteUrlCharsForDecoding(ref urlEncoded, ref base64, i + 3);
                }

                for (; (int*)i < (int*)n; i += 1)
                {
                    SubstituteUrlCharsForDecoding(ref urlEncoded, ref base64, i);
                }

                if (isFinalBlock)
                {
                    n = (IntPtr)base64Len;

                    // There will be a maximum of 2 padding chars.
                    if ((int*)i < (int*)n)
                    {
                        Pad(ref base64, i);

                        i += 1;
                        if ((int*)i < (int*)n)
                        {
                            Pad(ref base64, i);
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SubstituteUrlCharsForDecoding(ReadOnlySpan<char> urlEncoded, Span<byte> base64)
            {
                // Copy input into base64, fixing up '-' -> '+' and '_' -> '/' and add padding.

                ref var input = ref MemoryMarshal.GetReference(urlEncoded);
                ref var output = ref MemoryMarshal.GetReference(base64);

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)urlEncoded.Length;
                var m = i;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)(2 * Vector<ushort>.Count))
                {
                    m = (IntPtr)((int)(int*)n & ~(2 * Vector<ushort>.Count - 1));
                    for (; (int*)i < (int*)m; i += 2 * Vector<ushort>.Count)
                    {
                        ref var tmp = ref Unsafe.Add(ref input, i);
                        var charsVec1 = Unsafe.As<char, Vector<ushort>>(ref tmp);
                        var charsVec2 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref tmp, Vector<ushort>.Count));
                        var bytesVec = Vector.Narrow(charsVec1, charsVec2);

                        bytesVec = Substitute(bytesVec, (byte)'-', (byte)'+');
                        bytesVec = Substitute(bytesVec, (byte)'_', (byte)'/');

                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, i), bytesVec);
                    }
                }
#endif
                m = (IntPtr)((int)(int*)n & ~3);
                for (; (int*)i < (int*)m; i += 4)
                {
                    SubstituteUrlCharsForDecoding(ref input, ref output, i + 0);
                    SubstituteUrlCharsForDecoding(ref input, ref output, i + 1);
                    SubstituteUrlCharsForDecoding(ref input, ref output, i + 2);
                    SubstituteUrlCharsForDecoding(ref input, ref output, i + 3);
                }

                for (; (int*)i < (int*)n; i += 1)
                {
                    SubstituteUrlCharsForDecoding(ref input, ref output, i);
                }

                n = (IntPtr)base64.Length;

                // There will be a maximum of 2 padding chars.
                if ((int*)i < (int*)n)
                {
                    Pad(ref output, i);

                    i += 1;
                    if ((int*)i < (int*)n)
                    {
                        Pad(ref output, i);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int SubstituteUrlCharsForEncoding(Span<byte> base64, int count)
            {
                ref var r = ref MemoryMarshal.GetReference(base64);

                return SubstituteUrlCharsForEncoding(ref r, ref r, count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int SubstituteUrlCharsForEncoding(ReadOnlySpan<char> base64, Span<char> urlEncoded)
            {
                ref var input = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(base64));
                ref var output = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(urlEncoded));

                return SubstituteUrlCharsForEncoding(ref input, ref output, base64.Length);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int SubstituteUrlCharsForEncoding<T>(ref T base64, ref T urlEncoded, int base64Length) where T : struct
            {
                // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)base64Length;
                var m = i;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<T>.Count)
                {
                    m = (IntPtr)((int)(int*)n & ~(Vector<T>.Count - 1));
                    for (; (int*)i < (int*)m; i += Vector<T>.Count)
                    {
                        var vec = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref base64, i)));

                        if (typeof(T) == typeof(byte))
                        {
                            if (Vector.EqualsAny(vec, new Vector<T>((T)(object)(byte)'='))) break;

                            vec = Substitute<T>(vec, (T)(object)(byte)'+', (T)(object)(byte)'-');
                            vec = Substitute<T>(vec, (T)(object)(byte)'/', (T)(object)(byte)'_');
                        }
                        else if (typeof(T) == typeof(ushort))
                        {
                            if (Vector.EqualsAny(vec, new Vector<T>((T)(object)(ushort)'='))) break;

                            vec = Substitute<T>(vec, (T)(object)(ushort)'+', (T)(object)(ushort)'-');
                            vec = Substitute<T>(vec, (T)(object)(ushort)'/', (T)(object)(ushort)'_');
                        }
                        else
                        {
                            throw new NotSupportedException();  // just in case new types are introduced in the future
                        }

                        Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref urlEncoded, i)), vec);
                    }
                }
#endif
                // n is always a multiple of 4
                Debug.Assert((int)n % 4 == 0);
                for (; (int*)i < (int*)n; i += 4)
                {
                    if (SubstituteUrlCharsForEncoding(ref base64, ref urlEncoded, i + 0)) goto Exit0;
                    if (SubstituteUrlCharsForEncoding(ref base64, ref urlEncoded, i + 1)) goto Exit1;
                    if (SubstituteUrlCharsForEncoding(ref base64, ref urlEncoded, i + 2)) goto Exit2;
                    if (SubstituteUrlCharsForEncoding(ref base64, ref urlEncoded, i + 3)) goto Exit3;
                }
                goto Exit0;

            Exit3: i += 1;
            Exit2: i += 1;
            Exit1: i += 1;
            Exit0:
                return (int)(int*)i;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int SubstituteUrlCharsForEncoding(ReadOnlySpan<byte> base64, Span<char> urlEncoded)
            {
                // A subset of the ASCII-range can be assumed, so no need
                // to call the encoders necessary for byte -> char

                ref var input = ref MemoryMarshal.GetReference(base64);
                ref var output = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, ushort>(urlEncoded));

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)base64.Length;
                var m = i;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<byte>.Count)
                {
                    m = (IntPtr)((int)(int*)n & ~(Vector<byte>.Count - 1));
                    for (; (int*)i < (int*)m; i += Vector<byte>.Count)
                    {
                        var bytesVec = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref input, i));

                        if (Vector.EqualsAny(bytesVec, new Vector<byte>((byte)'=')))
                        {
                            break;
                        }

                        bytesVec = Substitute(bytesVec, (byte)'+', (byte)'-');
                        bytesVec = Substitute(bytesVec, (byte)'/', (byte)'_');

                        Vector.Widen(bytesVec, out Vector<ushort> charsVec1, out Vector<ushort> charsVec2);
                        ref var tmp = ref Unsafe.Add(ref output, i);
                        Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref tmp), charsVec1);
                        Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref tmp, Vector<ushort>.Count)), charsVec2);
                    }
                }
#endif
                // n is always a multiple of 4
                Debug.Assert((int)n == ((int)n & ~3));
                for (; (int*)i < (int*)n; i += 4)
                {
                    if (SubstituteUrlCharsForEncoding(ref input, ref output, i + 0)) goto Exit0;
                    if (SubstituteUrlCharsForEncoding(ref input, ref output, i + 1)) goto Exit1;
                    if (SubstituteUrlCharsForEncoding(ref input, ref output, i + 2)) goto Exit2;
                    if (SubstituteUrlCharsForEncoding(ref input, ref output, i + 3)) goto Exit3;
                }
                goto Exit0;

            Exit3: i += 1;
            Exit2: i += 1;
            Exit1: i += 1;
            Exit0:
                return (int)(int*)i;
            }
#if !NET461
            private static Vector<T> Substitute<T>(Vector<T> vector, T match, T substitution) where T : struct
                => Vector.ConditionalSelect(
                    Vector.Equals(vector, new Vector<T>(match)),
                    new Vector<T>(substitution),
                    vector);
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SubstituteUrlCharsForDecoding<TIn, TOut>(ref TIn urlEncoded, ref TOut base64, IntPtr idx)
                where TIn : struct
                where TOut : struct
            {
                TIn tmp = Unsafe.Add(ref urlEncoded, idx);
                int value = default;

                if (typeof(TIn) == typeof(byte))
                {
                    value = (byte)(object)tmp;
                }
                else if (typeof(TIn) == typeof(ushort))
                {
                    value = (ushort)(object)tmp;
                }
                else if (typeof(TIn) == typeof(char))
                {
                    value = (char)(object)tmp;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                var subst = value;

                if (value == '-')
                {
                    subst = '+';
                }
                else if (value == '_')
                {
                    subst = '/';
                }

                if (typeof(TOut) == typeof(byte))
                {
                    Unsafe.Add(ref base64, idx) = (TOut)(object)(byte)subst;
                }
                else if (typeof(TOut) == typeof(ushort))
                {
                    Unsafe.Add(ref base64, idx) = (TOut)(object)(ushort)subst;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void Pad<T>(ref T base64, IntPtr idx) where T : struct
            {
                if (typeof(T) == typeof(byte))
                {
                    Unsafe.Add(ref base64, idx) = (T)(object)(byte)'=';
                }
                else if (typeof(T) == typeof(ushort))
                {
                    Unsafe.Add(ref base64, idx) = (T)(object)(ushort)'=';
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool SubstituteUrlCharsForEncoding<TIn, TOut>(ref TIn base64, ref TOut urlEncoded, IntPtr idx)
                where TIn : struct
                where TOut : struct
            {
                TIn tmp = Unsafe.Add(ref base64, idx);
                int value = default;

                if (typeof(TIn) == typeof(byte))
                {
                    value = (byte)(object)tmp;
                }
                else if (typeof(TIn) == typeof(ushort))
                {
                    value = (ushort)(object)tmp;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                var subst = value;

                if (value == '+')
                {
                    subst = '-';
                }
                else if (value == '/')
                {
                    subst = '_';
                }
                else if (value == '=')
                {
                    return true;
                }

                if (typeof(TOut) == typeof(byte))
                {
                    Unsafe.Add(ref urlEncoded, idx) = (TOut)(object)(byte)subst;
                }
                else if (typeof(TOut) == typeof(ushort))
                {
                    Unsafe.Add(ref urlEncoded, idx) = (TOut)(object)(ushort)subst;
                }
                else
                {
                    throw new NotSupportedException();  // just in case new types are introduced in the future
                }

                return false;
            }
        }

        private static class ThrowHelper
        {
            public static void ThrowArgumentNullException(ExceptionArgument argument)
            {
                throw GetArgumentNullException(argument);
            }

            public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
            {
                throw GetArgumentOutOfRangeException(argument);
            }

            public static void ThrowInvalidCountOffsetOrLengthException(ExceptionArgument arg1, ExceptionArgument arg2, ExceptionArgument arg3)
            {
                throw GetInvalidCountOffsetOrLengthException(arg1, arg2, arg3);
            }

            public static void ThrowMalformedInputException(int inputLength)
            {
                throw GetMalformdedInputException(inputLength);
            }

            public static void ThrowOperationNotDone(OperationStatus status)
            {
                throw GetOperationNotDoneException(status);
            }

            public static ArgumentNullException GetArgumentNullException(ExceptionArgument argument)
            {
                return new ArgumentNullException(GetArgumentName(argument));
            }

            public static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument)
            {
                return new ArgumentOutOfRangeException(GetArgumentName(argument));
            }

            public static ArgumentException GetInvalidCountOffsetOrLengthException(ExceptionArgument arg1, ExceptionArgument arg2, ExceptionArgument arg3)
            {
                return new ArgumentException(EncoderResources.FormatWebEncoders_InvalidCountOffsetOrLength(
                    GetArgumentName(arg1),
                    GetArgumentName(arg2),
                    GetArgumentName(arg3)));
            }

            private static Exception GetOperationNotDoneException(OperationStatus status)
            {
                switch (status)
                {
                    case OperationStatus.DestinationTooSmall:
                        return new InvalidOperationException(EncoderResources.WebEncoders_DestinationTooSmall);
                    case OperationStatus.InvalidData:
                        return new FormatException(EncoderResources.WebEncoders_InvalidInput);
                    default:                                // This case won't happen.
                        throw new NotSupportedException();  // Just in case new states are introduced
                }
            }

            private static string GetArgumentName(ExceptionArgument argument)
            {
                Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                    "The enum value is not defined, please check the ExceptionArgument Enum.");

                return argument.ToString();
            }

            private static FormatException GetMalformdedInputException(int inputLength)
            {
                return new FormatException(EncoderResources.FormatWebEncoders_MalformedInput(inputLength));
            }
        }

        private enum ExceptionArgument
        {
            input,
            buffer,
            output,
            count,
            offset,
            bufferOffset,
            outputOffset
        }
    }
}
