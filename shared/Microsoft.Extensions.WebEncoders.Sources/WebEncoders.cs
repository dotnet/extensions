// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.WebEncoders.Sources;

#if !NETCOREAPP2_1
using System.Runtime.InteropServices;
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

            // Special-case empty input
            if (count == 0)
            {
                return EmptyBytes;
            }

            // Create array large enough for the Base64 characters, not just shorter Base64-URL-encoded form.
            var arraySizeRequired = GetArraySizeRequiredToDecodeCore(count);

            var buffer = new Buffer<char>(arraySizeRequired);
            try
            {
                return Base64UrlDecodeCore(input.AsSpan(offset, count), buffer.AsSpan());
            }
            finally
            {
                buffer.Dispose();
            }
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

            var arraySizeRequired = GetArraySizeRequiredToDecodeCore(count);

            if (buffer.Length - bufferOffset < arraySizeRequired)
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.bufferOffset, ExceptionArgument.input);
            }

            return Base64UrlDecodeCore(input.AsSpan(offset, count), buffer.AsSpan(bufferOffset, arraySizeRequired));
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

            return GetArraySizeRequiredToDecodeCore(count);
        }

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
        public static unsafe string Base64UrlEncode(byte[] input, int offset, int count)
        {
            if (input == null
                || (uint)offset > (uint)input.Length
                || (uint)count > (uint)(input.Length - offset))
            {
                ThrowInvalidArguments(input, offset, count);
            }

            // Special-case empty input
            if (count == 0)
            {
                return string.Empty;
            }

            var arraySizeRequired = GetArraySizeRequiredToEncodeCore(count);
#if NETCOREAPP2_1
            var buffer = new Buffer<char>(arraySizeRequired);
            try
            {
                var output = buffer.AsSpan();
                output = Base64UrlEncodeCore(input.AsSpan(offset, count), output);

                // Todo: use string.Create
                return new String(output);
#else
            var buffer = new char[arraySizeRequired];
            var numBase64Chars = Base64UrlEncodeCore(input, offset, count, buffer, 0);

            return new String(buffer, 0, numBase64Chars);
#endif
#if NETCOREAPP2_1
            }
            finally
            {
                buffer.Dispose();
            }
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

            var arraySizeRequired = GetArraySizeRequiredToEncodeCore(count);
            if (output.Length - outputOffset < arraySizeRequired)
            {
                ThrowHelper.ThrowInvalidCountOffsetOrLengthException(ExceptionArgument.count, ExceptionArgument.outputOffset, ExceptionArgument.output);
            }

            // Special-case empty input.
            if (count == 0)
            {
                return 0;
            }

#if NETCOREAPP2_1
            var buffer = output.AsSpan(outputOffset);
            buffer = Base64UrlEncodeCore(input.AsSpan(offset, count), buffer);

            return buffer.Length;
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
        public static int GetArraySizeRequiredToEncode(int count)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            return GetArraySizeRequiredToEncodeCore(count);
        }

        private static byte[] Base64UrlDecodeCore(ReadOnlySpan<char> input, Span<char> buffer)
        {
            UrlDecodeInternal(input, buffer);

#if NETCOREAPP2_1
            var maxDecodedSize = Base64.GetMaxDecodedFromUtf8Length(buffer.Length);
            var resultBuffer = new Buffer<byte>(maxDecodedSize);
            try
            {
                var resultSpan = resultBuffer.AsSpan();

                // Decode.
                // If the caller provided invalid base64 chars, they'll be caught here.
                // Todo: Ask how to handle result
                Convert.TryFromBase64Chars(buffer, resultSpan, out int bytesWritten);
                return resultSpan.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                resultBuffer.Dispose();
            }
#else
            // Decode.
            // If the caller provided invalid base64 chars, they'll be caught here.
            return Convert.FromBase64CharArray(buffer.ToArray(), 0, buffer.Length);
#endif
        }

#if NETCOREAPP2_1
        private static Span<char> Base64UrlEncodeCore(ReadOnlySpan<byte> input, Span<char> buffer)
        {
            // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

            // Start with default Base64 encoding.
            // Todo: Ask how to handle result
            var res = Convert.TryToBase64Chars(input, buffer, out int charsWritten);
            var output = buffer.Slice(0, charsWritten);
            
            return UrlEncodeInternal(output);
#else
        private static int Base64UrlEncodeCore(byte[] input, int offset, int count, char[] buffer, int bufferOffset)
        {
            // Start with default Base64 encoding.
            var numBase64Chars = Convert.ToBase64CharArray(input, offset, count, buffer, bufferOffset);
            var output = UrlEncodeInternal(buffer.AsSpan(bufferOffset, numBase64Chars));

            return output.Length;
#endif
        }

        // internal to make this testable
        internal static unsafe void UrlDecodeInternal(ReadOnlySpan<char> input, Span<char> buffer)
        {
            // PERF: &input[0] is faster than &MemoryMarshal.GetReference(input);
            fixed (char* pInput = &input[0])
            fixed (char* pBuffer = &buffer[0])
            {
                // Copy input into buffer, fixing up '-' -> '+' and '_' -> '/'.
                var i = 0;
                for (; i < input.Length; ++i)
                {
                    var ch = pInput[i];
                    if (ch == '-')
                    {
                        pBuffer[i] = '+';
                    }
                    else if (ch == '_')
                    {
                        pBuffer[i] = '/';
                    }
                    else
                    {
                        pBuffer[i] = ch;
                    }
                }

                // Add the padding characters back.
                for (; i < buffer.Length; ++i)
                {
                    pBuffer[i] = '=';
                }
            }
        }

        // internal to make this testable
        internal static Span<char> UrlEncodeInternal(Span<char> output)
        {
            for (var i = 0; i < output.Length; ++i)
            {
                var ch = output[i];
                if (ch == '+')
                {
                    output[i] = '-';
                }
                else if (ch == '/')
                {
                    output[i] = '_';
                }
                else if (ch == '=')
                {
                    // We've reached a padding character; truncate the remainder.
                    return output.Slice(0, i);
                }
            }

            return output;
        }

        private static int GetArraySizeRequiredToDecodeCore(int count)
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

        private static int GetArraySizeRequiredToEncodeCore(int count)
        {
            if (count == 0)
            {
                return 0;
            }

            var numWholeOrPartialInputBlocks = checked(count + 2) / 3;
            return checked(numWholeOrPartialInputBlocks * 4);
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
