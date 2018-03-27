// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using Microsoft.Extensions.WebEncoders.Sources;

#if NETCOREAPP2_1
using System.Buffers;
using System.Buffers.Text;
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
        private const int MaxEncodedLength = int.MaxValue / 4 * 3;  // encode inflates the data by 4/3
        private static readonly byte[] EmptyBytes = new byte[0];

        // TODO: XML Comment
        public static int Base64UrlDecode(ReadOnlySpan<char> base64Url, Span<byte> data)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                return 0;
            }

            var buffer = new Buffer<byte>(base64Url.Length);
            try
            {
                var base64UrlBytes = buffer.AsSpan();
                EncodingHelper.GetBytes(base64Url, base64UrlBytes);
                var status = Base64UrlDecode(base64UrlBytes, data, out int bytesConsumed, out int bytesWritten);

                if (status == OperationStatus.InvalidData)
                {
                    ThrowHelper.ThrowMalformedInputException(base64Url.Length);
                }
                else if (status != OperationStatus.Done)
                {
                    ThrowHelper.ThrowInvalidOperationException(status);
                }

                return bytesWritten;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        // TODO: XML Comment
        public static byte[] Base64UrlDecode(ReadOnlySpan<char> base64Url)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                return EmptyBytes;
            }

            // Create array large enough for the Base64 characters, not just shorter Base64-URL-encoded form.
            var arraySizeRequired = GetBufferSizeRequiredToUrlDecode(base64Url.Length);
            var buffer = new Buffer<byte>(arraySizeRequired);
            try
            {
                var span = buffer.AsSpan();
                var written = Base64UrlDecode(base64Url, span);
                return span.Slice(0, written).ToArray();
            }
            finally
            {
                buffer.Dispose();
            }
        }

#if NETCOREAPP2_1
        // TODO: XML Comment
        public static OperationStatus Base64UrlDecode(ReadOnlySpan<byte> base64Url, Span<byte> data, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            // Special-case empty input
            if (base64Url.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            // Create array large enough for the Base64 characters, not just shorter Base64-URL-encoded form.
            var bufferSizeRequired = isFinalBlock ? GetBufferSizeRequiredToUrlDecode(base64Url.Length) : base64Url.Length;
            var buffer = new Buffer<byte>(bufferSizeRequired);
            try
            {
                var base64Encoded = buffer.AsSpan();
                EncodingHelper.UrlDecode(base64Url, base64Encoded, isFinalBlock);

                // If the caller provided invalid base64 chars, they'll be caught here.
                var status = Base64.DecodeFromUtf8(base64Encoded, data, out bytesConsumed, out bytesWritten, isFinalBlock);

                // Fix bytesConsumed to match the input 'base64Url' (and not the 'base64Encoded')
                bytesConsumed = base64Url.Length - (base64Encoded.Length - bytesConsumed);

                return status;
            }
            finally
            {
                buffer.Dispose();
            }
        }
#endif

        /// <summary>
        /// Decodes a base64url-encoded string.
        /// </summary>
        /// <param name="input">The base64url-encoded input to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input) => Base64UrlDecode(input, 0, input.Length);

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

            // Assumption: input is base64url encoded without padding and contains no whitespace.

            var arraySizeRequired = GetBufferSizeRequiredToUrlDecode(count);

            if (buffer.Length - bufferOffset < arraySizeRequired)
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.bufferOffset, ExceptionArgument.input);
            }

            return Base64UrlDecodeCore(input.AsSpan(offset, count), buffer.AsSpan(bufferOffset, arraySizeRequired));
        }

        private static byte[] Base64UrlDecodeCore(ReadOnlySpan<char> input, Span<char> buffer)
        {
            EncodingHelper.UrlDecode(input, buffer);

#if NETCOREAPP2_1
            var maxDecodedSize = Base64.GetMaxDecodedFromUtf8Length(buffer.Length);
            var outputBuffer = new Buffer<byte>(maxDecodedSize);
            try
            {
                var output = outputBuffer.AsSpan();

                // Decode.
                // If the caller provided invalid base64 chars, they'll be caught here.
                // TODO: try  byte-based variant
                Convert.TryFromBase64Chars(buffer, output, out int bytesWritten);
                return output.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                outputBuffer.Dispose();
            }
#else
            // Decode.
            // If the caller provided invalid base64 chars, they'll be caught here.
            return Convert.FromBase64CharArray(buffer.ToArray(), 0, buffer.Length);
#endif
        }

        /// <summary>
        /// Gets the minimum <c>char[]</c> size required for decoding of <paramref name="count"/> characters
        /// with the <see cref="Base64UrlDecode(string, int, char[], int, int)"/> method.
        /// </summary>
        /// <param name="count">The number of characters to decode.</param>
        /// <returns>
        /// The minimum <c>char[]</c> size required for decoding  of <paramref name="count"/> characters.
        /// </returns>
        public static int GetArraySizeRequiredToDecode(int count)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            return GetBufferSizeRequiredToUrlDecode(count);
        }

        // TODO: XML Comment
        public static int Base64UrlEncode(ReadOnlySpan<byte> data, Span<char> base64Url)
        {
            // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

            // Special-case empty input
            if (data.IsEmpty)
            {
                return 0;
            }

            var bufferSizeRequired = GetBufferSizeRequiredToBase64Encode(data.Length);
            var buffer = new Buffer<byte>(bufferSizeRequired);
            try
            {
                var base64Bytes = buffer.AsSpan();
                var status = Base64UrlEncode(data, base64Bytes, out int consumed, out int written);

                if (status == OperationStatus.Done)
                {
                    EncodingHelper.GetChars(base64Bytes.Slice(0, written), base64Url);
                }
                else
                {
                    ThrowHelper.ThrowInvalidOperationException(status);
                }

                return written;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        // TODO: XML Comment
        public static unsafe string Base64UrlEncode(ReadOnlySpan<byte> data)
        {
            // Special-case empty input
            if (data.IsEmpty)
            {
                return string.Empty;
            }

#if NETCOREAPP2_1
            var base64UrlLen = GetBufferSizeRequiredToUrlEncode(data.Length);
            fixed (byte* ptr = &MemoryMarshal.GetReference(data))
            {
                return string.Create(base64UrlLen, (Ptr: (IntPtr)ptr, data.Length), (base64Url, state) =>
                {
                    var bytes = new ReadOnlySpan<byte>(state.Ptr.ToPointer(), state.Length);
                    Base64UrlEncode(bytes, base64Url);
                });
            }
#else
            return Base64UrlEncode(input.ToArray());
#endif
        }

#if NETCOREAPP2_1
        // TODO: XML Comment
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
                var span = base64Url.Slice(0, bytesWritten);
                bytesWritten = EncodingHelper.UrlEncode(span);
            }

            return status;
        }
#endif

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(byte[] input) => Base64UrlEncode(input, 0, input.Length);

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
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

#if NETCOREAPP2_1
            return Base64UrlEncode(input.AsSpan(offset, count));
#else
            // Special-case empty input
            if (count == 0)
            {
                return string.Empty;
            }

            var arraySizeRequired = GetArraySizeRequiredToEncodeCore(count);
            var buffer = new char[arraySizeRequired];
            var numBase64Chars = Base64UrlEncodeCore(input, offset, count, buffer, 0);

            return new String(buffer, 0, numBase64Chars);
#endif
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
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

            var bufferSizeRequired = GetBufferSizeRequiredToBase64Encode(count);
            if (output.Length - outputOffset < bufferSizeRequired)
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.outputOffset, ExceptionArgument.output);
            }

            // Special-case empty input.
            if (count == 0)
            {
                return 0;
            }

#if NETCOREAPP2_1
            return Base64UrlEncode(input.AsSpan(offset, count), output.AsSpan(outputOffset));
#else
            return Base64UrlEncodeCore(input, offset, count, output, outputOffset);
#endif
        }

        /// <summary>
        /// Get the minimum output <c>char[]</c> size required for encoding <paramref name="count"/>
        /// <see cref="byte"/>s with the <see cref="Base64UrlEncode(byte[], int, char[], int, int)"/> method.
        /// </summary>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>
        /// The minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="byte"/>s.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetArraySizeRequiredToEncode(int count)
        {
            if (count < 0 || count > MaxEncodedLength)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            return GetBufferSizeRequiredToBase64Encode(count);
        }

#if !NETCOREAPP2_1
        private static int Base64UrlEncodeCore(byte[] input, int offset, int count, char[] buffer, int bufferOffset)
        {
            // Start with default Base64 encoding.
            var numBase64Chars = Convert.ToBase64CharArray(input, offset, count, buffer, bufferOffset);
            return EncodingHelper.UrlEncode(buffer.AsSpan(bufferOffset, numBase64Chars));
        }
#endif

        private static int GetBufferSizeRequiredToUrlDecode(int count)
        {
            if (count == 0)
            {
                return 0;
            }

            var numPaddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);
            var arraySizeRequired = checked(count + numPaddingCharsToAdd);
            Debug.Assert(arraySizeRequired % 4 == 0, "Invariant: Array length must be a multiple of 4.");

            return arraySizeRequired;
        }

        private static int GetBufferSizeRequiredToBase64Encode(int dataLength)
        {
            if (dataLength == 0)
            {
                return 0;
            }

            // overflow conditions are already eliminated, so 'checked' is not necessary
            var numWholeOrPartialInputBlocks = (dataLength + 2) / 3;
            return numWholeOrPartialInputBlocks * 4;
        }

        private static int GetBufferSizeRequiredToUrlEncode(int dataLength)
        {
            return GetBufferSizeRequiredToBase64Encode(dataLength) - GetNumBase64PaddingCharsAddedByEncode(dataLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumBase64PaddingCharsToAddForDecode(int inputLength)
        {
            // Calculation is:
            // switch (inputLength % 4)
            // 0 -> 0
            // 2 -> 2
            // 3 -> 1
            // default -> format exception

            var result = 1;
            var mod = inputLength & 3;

            if (mod == 1)
            {
                ThrowHelper.ThrowMalformedInputException(inputLength);
            }
            else if (mod == 0 || mod == 2)
            {
                result = mod;
            }

            return result;
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

        private unsafe struct Buffer<T> where T : struct
        {
            private const int MaxStack = 32;
            private T[] _arrayToReturnToPool;
            private readonly int _size;
            private fixed long _buffer[MaxStack];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Buffer(int size)
            {
                _arrayToReturnToPool = null;
                _size = size;

                // T is only char or byte
                int sizeOfT = typeof(T) == typeof(byte) ? 1 : 2;
                if (size > MaxStack * sizeof(long) / sizeOfT)
                {
#if NETCOREAPP2_1
                    _arrayToReturnToPool = ArrayPool<T>.Shared.Rent(size);
#else
                    _arrayToReturnToPool = new T[size];
#endif
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan()
            {
                Span<T> res;

                if (_arrayToReturnToPool != null)
                {
                    res = new Span<T>(_arrayToReturnToPool, 0, _size);
                }
                else
                {
                    fixed (long* buffer = _buffer)
                    {
                        res = new Span<T>(buffer, _size);
                    }
                }

                return res;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (_arrayToReturnToPool != null)
                {
#if NETCOREAPP2_1
                    ArrayPool<T>.Shared.Return(_arrayToReturnToPool);
#endif
                    _arrayToReturnToPool = null;
                }
            }
        }

        // internal to make this testable
        internal static unsafe class EncodingHelper
        {
            public static void GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
            {
                // A subset of the ASCII-range can be assumed, so no need
                // to call the encoders

                ref var source = ref MemoryMarshal.GetReference(bytes);
                ref var target = ref MemoryMarshal.GetReference(chars);

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)bytes.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<byte>.Count)
                {
                    var m = n - Vector<byte>.Count;
                    for (; (int*)i < (int*)m; i += Vector<byte>.Count)
                    {
                        var bytesVec = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref source, i));
                        Vector.Widen(bytesVec, out Vector<ushort> charsVec1, out Vector<ushort> charsVec2);

                        ref var tmp = ref Unsafe.Add(ref target, i);
                        Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref tmp), charsVec1);
                        Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref tmp, Vector<ushort>.Count)), charsVec2);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    Unsafe.Add(ref target, i) = (char)Unsafe.Add(ref source, i);
                }
            }

            public static void GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                // A subset of the ASCII-range can be assumed, so no need
                // to call the encoders.

                ref var source = ref MemoryMarshal.GetReference(chars);
                ref var target = ref MemoryMarshal.GetReference(bytes);

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)chars.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)(Vector<ushort>.Count * 2))
                {
                    var m = n - 2 * Vector<ushort>.Count;
                    for (; (int*)i < (int*)m; i += 2 * Vector<ushort>.Count)
                    {
                        ref var tmp = ref Unsafe.Add(ref source, i);
                        var charsVec1 = Unsafe.As<char, Vector<ushort>>(ref tmp);
                        var charsVec2 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref tmp, Vector<ushort>.Count));
                        var bytesVec = Vector.Narrow(charsVec1, charsVec2);

                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref target, i), bytesVec);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    Unsafe.Add(ref target, i) = (byte)Unsafe.Add(ref source, i);
                }
            }

            public static void UrlDecode(ReadOnlySpan<byte> input, Span<byte> output, bool isFinalBlock = true)
            {
                // Copy input into buffer, fixing up '-' -> '+' and '_' -> '/'.

                ref var inputRef = ref MemoryMarshal.GetReference(input);
                ref var outputRef = ref MemoryMarshal.GetReference(output);

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)input.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<byte>.Count)
                {
                    var m = n - Vector<byte>.Count;
                    for (; (int*)i < (int*)m; i += Vector<byte>.Count)
                    {
                        var inputVec = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref inputRef, i));

                        inputVec = Substitute(inputVec, (byte)'-', (byte)'+');
                        inputVec = Substitute(inputVec, (byte)'_', (byte)'/');

                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputRef, i), inputVec);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    var value = Unsafe.Add(ref inputRef, i);
                    var subst = value;

                    if (value == '-')
                    {
                        subst = (byte)'+';
                    }
                    else if (value == '_')
                    {
                        subst = (byte)'/';
                    }

                    Unsafe.Add(ref outputRef, i) = subst;
                }

                if (isFinalBlock)
                {
                    output.Slice((int)i).Fill((byte)'=');
                }
            }

            public static void UrlDecode(ReadOnlySpan<char> input, Span<char> output)
            {
                // Copy input into buffer, fixing up '-' -> '+' and '_' -> '/'.

                ref var inputRef = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, ushort>(input));
                ref var outputRef = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, ushort>(output));

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)input.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<ushort>.Count)
                {
                    var m = n - Vector<ushort>.Count;
                    for (; (int*)i < (int*)m; i += Vector<ushort>.Count)
                    {
                        var inputVec = Unsafe.As<ushort, Vector<ushort>>(ref Unsafe.Add(ref inputRef, i));

                        inputVec = Substitute(inputVec, '-', '+');
                        inputVec = Substitute(inputVec, '_', '/');

                        Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref outputRef, i)), inputVec);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    var value = Unsafe.Add(ref inputRef, i);
                    var subst = value;

                    if (value == '-')
                    {
                        subst = '+';
                    }
                    else if (value == '_')
                    {
                        subst = '/';
                    }

                    Unsafe.Add(ref outputRef, i) = subst;
                }

                output.Slice((int)i).Fill('=');
            }

            public static int UrlEncode(Span<byte> base64)
            {
                // Fixing up '+' -> '-' and '/' -> '_', and truncate at '='

                ref var data = ref MemoryMarshal.GetReference(base64);

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)base64.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<byte>.Count)
                {
                    var m = n - Vector<byte>.Count;
                    for (; (int*)i < (int*)m; i += Vector<byte>.Count)
                    {
                        var vec = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref data, i));

                        if (Vector.EqualsAny(vec, new Vector<byte>((byte)'=')))
                        {
                            break;
                        }

                        vec = Substitute(vec, (byte)'+', (byte)'-');
                        vec = Substitute(vec, (byte)'/', (byte)'_');

                        Unsafe.WriteUnaligned(ref Unsafe.Add(ref data, i), vec);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    ref var value = ref Unsafe.Add(ref data, i);

                    if (value == '+')
                    {
                        value = (byte)'-';
                    }
                    else if (value == '/')
                    {
                        value = (byte)'_';
                    }
                    else if (value == '=')
                    {
                        break;
                    }
                }

                return (int)i;
            }

            public static void UrlEncode(ReadOnlySpan<char> input, Span<char> output)
            {
                ref ushort inputRef = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, ushort>(input));
                ref ushort outputRef = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, ushort>(output));

                var i = (IntPtr)0;      // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
                var n = (IntPtr)input.Length;
#if !NET461
                if (Vector.IsHardwareAccelerated && (int*)n >= (int*)Vector<ushort>.Count)
                {
                    var m = n - Vector<ushort>.Count;
                    for (; (int*)i < (int*)m; i += Vector<ushort>.Count)
                    {
                        var vec = Unsafe.As<ushort, Vector<ushort>>(ref Unsafe.Add(ref inputRef, i));

                        if (Vector.EqualsAny(vec, new Vector<ushort>('=')))
                        {
                            break;
                        }

                        vec = Substitute(vec, '+', '-');
                        vec = Substitute(vec, '/', '_');

                        Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref outputRef, i)), vec);
                    }
                }
#endif
                for (; (int*)i < (int*)n; i += 1)
                {
                    var value = Unsafe.Add(ref inputRef, i);
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
                        break;
                    }

                    Unsafe.Add(ref outputRef, i) = subst;
                }
            }

#if !NET461
            private static Vector<T> Substitute<T>(Vector<T> vector, T match, T substitution) where T : struct
                => Vector.ConditionalSelect(
                    Vector.Equals(vector, new Vector<T>(match)),
                    new Vector<T>(substitution),
                    vector);
#endif
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

#if NETCOREAPP2_1
            public static void ThrowInvalidOperationException(OperationStatus status)
            {
                throw new InvalidOperationException(status.ToString());
            }
#endif

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
