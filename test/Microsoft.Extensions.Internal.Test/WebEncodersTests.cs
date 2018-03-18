// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Buffers;
using Xunit;
using System.Text;

namespace Microsoft.Extensions.Internal
{
    public class WebEncodersTests
    {
        // Taken from https://github.com/aspnet/HttpAbstractions/pull/926
        [Fact]
        public void DataOfVariousLength_RoundTripCorrectly()
        {
            for (var length = 0; length < 256; length++)
            {
                var data = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    data[i] = (byte)(5 + length + (i * 23));
                }

                string text = WebEncoders.Base64UrlEncode(data);
                byte[] result = WebEncoders.Base64UrlDecode(text);

                for (var i = 0; i < length; i++)
                {
                    Assert.Equal(data[i], result[i]);
                }
            }
        }

        [Fact]
        public void DataOfVariousLengthAsSpan_RoundTripCorrectly()
        {
            for (var length = 0; length < 256; length++)
            {
                var data = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    data[i] = (byte)(5 + length + (i * 23));
                }

                var num = WebEncoders.GetArraySizeRequiredToEncode(data.Length);
                var utf8Buffer = new byte[num].AsSpan();
                var status = WebEncoders.Base64UrlEncode(data, utf8Buffer, out int bytesConsumed, out int bytesWritten);
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(data.Length, bytesConsumed);

                utf8Buffer = utf8Buffer.Slice(0, bytesWritten);
                num = WebEncoders.GetArraySizeRequiredToDecode(utf8Buffer.Length);
                var byteBuffer = new byte[num].AsSpan();
                status = WebEncoders.Base64UrlDecode(utf8Buffer, byteBuffer, out bytesConsumed, out bytesWritten);
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(utf8Buffer.Length, bytesConsumed);
                var result = byteBuffer.Slice(0, bytesWritten).ToArray();

                for (var i = 0; i < length; i++)
                {
                    Assert.Equal(data[i], result[i]);
                }
            }
        }

        [Theory]
        [InlineData("", 1, 0)]
        [InlineData("", 0, 1)]
        [InlineData("0123456789", 9, 2)]
        [InlineData("0123456789", Int32.MaxValue, 2)]
        [InlineData("0123456789", 9, -1)]
        public void Base64UrlDecode_BadOffsets(string input, int offset, int count)
        {
            // Act & assert
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                var retVal = WebEncoders.Base64UrlDecode(input, offset, count);
            });
        }

        [Theory]
        [InlineData("x")]
        [InlineData("(x)")]
        public void Base64UrlDecode_MalformedInput(string input)
        {
            // Act & assert
            Assert.Throws<FormatException>(() =>
            {
                var retVal = WebEncoders.Base64UrlDecode(input);
            });
        }

        [Fact]
        public void Base64UrlDecodeAsSpan_InputIsEmptyReturns0()
        {
            var input = string.Empty.AsSpan();
            var output = new byte[100].AsSpan();

            var result = WebEncoders.Base64UrlDecode(input, output);

            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("123456qwerty++//X+/x", "123456qwerty--__X-_x")]
        [InlineData("123456qwerty++//X+/xxw==", "123456qwerty--__X-_xxw")]
        [InlineData("123456qwerty++//X+/xxw0=", "123456qwerty--__X-_xxw0")]
        public void Base64UrlEncode_And_Decode(string base64Input, string expectedBase64Url)
        {
            // Arrange
            byte[] input = new byte[3].Concat(Convert.FromBase64String(base64Input)).Concat(new byte[2]).ToArray();

            // Act & assert - 1
            string actualBase64Url = WebEncoders.Base64UrlEncode(input, 3, input.Length - 5); // also helps test offsets
            Assert.Equal(expectedBase64Url, actualBase64Url);

            // Act & assert - 2
            // Verify that values round-trip
            byte[] roundTripped = WebEncoders.Base64UrlDecode("xx" + actualBase64Url + "yyy", 2, actualBase64Url.Length); // also helps test offsets
            string roundTrippedAsBase64 = Convert.ToBase64String(roundTripped);
            Assert.Equal(roundTrippedAsBase64, base64Input);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("123456qwerty++//X+/x", "123456qwerty--__X-_x")]
        [InlineData("123456qwerty++//X+/xxw==", "123456qwerty--__X-_xxw")]
        [InlineData("123456qwerty++//X+/xxw0=", "123456qwerty--__X-_xxw0")]
        public void Base64UrlEncode_And_Decode_WithBufferOffsets(string base64Input, string expectedBase64Url)
        {
            // Arrange
            var input = new byte[3].Concat(Convert.FromBase64String(base64Input)).Concat(new byte[2]).ToArray();
            var buffer = new char[30];
            var output = new char[30];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = '^';
                output[i] = '^';
            }

            // Act 1
            var numEncodedChars =
                WebEncoders.Base64UrlEncode(input, offset: 3, output: output, outputOffset: 4, count: input.Length - 5);

            // Assert 1
            var encodedString = new string(output, startIndex: 4, length: numEncodedChars);
            Assert.Equal(expectedBase64Url, encodedString);

            // Act 2
            var roundTripInput = new string(output);
            var roundTripped =
                WebEncoders.Base64UrlDecode(roundTripInput, offset: 4, buffer: buffer, bufferOffset: 5, count: numEncodedChars);

            // Assert 2, verify that values round-trip
            var roundTrippedAsBase64 = Convert.ToBase64String(roundTripped);
            Assert.Equal(roundTrippedAsBase64, base64Input);
        }

        [Theory]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, 1)]
        [InlineData(10, 9, 2)]
        [InlineData(10, Int32.MaxValue, 2)]
        [InlineData(10, 9, -1)]
        public void Base64UrlEncode_BadOffsets(int inputLength, int offset, int count)
        {
            // Arrange
            byte[] input = new byte[inputLength];

            // Act & assert
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                var retVal = WebEncoders.Base64UrlEncode(input, offset, count);
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(6, 8)]
        [InlineData(7, 8)]
        public void GetArraySizeRequiredToDecode(int inputLength, int expectedLength)
        {
            var result = WebEncoders.GetArraySizeRequiredToDecode(inputLength);

            Assert.Equal(expectedLength, result);
        }

        [Fact]
        public void GetArraySizeRequiredToDecode_NegativeInputLength_Throws()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => WebEncoders.GetArraySizeRequiredToDecode(-1));
            Assert.Equal("count", exception.ParamName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void GetArraySizeRequiredToDecode_MalformedInputLength(int inputLength)
        {
            Assert.Throws<FormatException>(() =>
            {
                var retVal = WebEncoders.GetArraySizeRequiredToDecode(inputLength);
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 8)]
        [InlineData(6, 8)]
        [InlineData(7, 12)]
        [InlineData(16, 24)]
        public void GetArraySizeRequiredToEncode(int inputLength, int expectedLength)
        {
            var result = WebEncoders.GetArraySizeRequiredToEncode(inputLength);

            Assert.Equal(expectedLength, result);
        }

        [Fact]
        public void GetArraySizeRequiredToEncode_NegativeInputLength_Throws()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => WebEncoders.GetArraySizeRequiredToEncode(-1));
            Assert.Equal("count", exception.ParamName);
        }

        [Fact]
        public void GetArraySizeRequiredToEncode_InputLengthTooBig_Throws()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => WebEncoders.GetArraySizeRequiredToEncode((int.MaxValue / 4) * 3 + 1));
            Assert.Equal("count", exception.ParamName);
        }

        // Taken from https://github.com/aspnet/HttpAbstractions/pull/926
        [Theory]
        [InlineData("_", "/")]
        [InlineData("-", "+")]
        [InlineData("a-b-c", "a+b+c=")]
        [InlineData("a_b_c_d", "a/b/c/d=")]
        [InlineData("a-b_c", "a+b/c==")]
        [InlineData("a-b_c-d", "a+b/c+d=")]
        [InlineData("abcd", "abcd")]
        public void SubstituteUrlCharsForDecoding_ReturnsValid_Base64String(string text, string expectedValue)
        {
// To test the alternate code-path
#if NETCOREAPP2_1
            // Arrange
            Span<byte> bytes = stackalloc byte[text.Length];
            Encoding.ASCII.GetBytes(text.AsSpan(), bytes);
            Span<byte> expected = stackalloc byte[expectedValue.Length];
            Encoding.ASCII.GetBytes(expectedValue.AsSpan(), expected);
            Span<byte> result = stackalloc byte[expectedValue.Length];

            // Act
            WebEncoders.UrlCharsHelper.SubstituteUrlCharsForDecoding(bytes, result);

            // Assert
            Assert.True(expected.SequenceEqual(result));
#else
            // Arrange
            var buffer = new char[expectedValue.Length];

            // Act
            WebEncoders.UrlCharsHelper.SubstituteUrlCharsForDecoding(text.AsSpan(), buffer);

            // Assert
            Assert.Equal(expectedValue, new string(buffer));
#endif
        }

        // Taken from https://github.com/aspnet/HttpAbstractions/pull/926
        // Input length must be a multiple of 4
        [Theory]
        [InlineData("    ", "    ")]
        [InlineData("+   ", "-   ")]
        [InlineData("/   ", "_   ")]
        [InlineData("=   ", "")]
        [InlineData("==  ", "")]
        [InlineData("a+b+c+==", "a-b-c-")]
        [InlineData("a/b/c== ", "a_b_c")]
        [InlineData("a+b/c== ", "a-b_c")]
        [InlineData("a+b/c   ", "a-b_c   ")]
        [InlineData("abcd", "abcd")]
        public void SubstituteUrlCharsForEncoding_Replaces_UrlEncodableCharacters(string base64EncodedValue, string expectedValue)
        {
// To test the alternate code-path
#if NETCOREAPP2_1
            // Arrange
            Span<byte> bytes = stackalloc byte[base64EncodedValue.Length];
            Encoding.ASCII.GetBytes(base64EncodedValue.AsSpan(), bytes);
            Span<byte> expected = stackalloc byte[expectedValue.Length];
            Encoding.ASCII.GetBytes(expectedValue.AsSpan(), expected);

            // Act
            var result = WebEncoders.UrlCharsHelper.SubstituteUrlCharsForEncoding(bytes, bytes.Length);

            // Assert
            Assert.True(expected.SequenceEqual(bytes.Slice(0, result)));
#else
            // Arrange
            var buffer = base64EncodedValue.ToCharArray();

            // Act
            var result = WebEncoders.UrlCharsHelper.SubstituteUrlCharsForEncoding(buffer, buffer);

            // Assert
            Assert.Equal(expectedValue.Length, result);
            for (var i = 0; i < result; i++)
            {
                Assert.Equal(expectedValue[i], buffer[i]);
            }
#endif
        }

        [Fact]
        public void Base64UrlDecode_BufferChain()
        {
            // Arrange
            var data = new byte[20];
            var rnd = new Random(0);
            rnd.NextBytes(data);
            var base64UrlString = WebEncoders.Base64UrlEncode(data);
            var base64Url = new byte[base64UrlString.Length];
            Encoding.ASCII.GetBytes(base64UrlString, 0, base64UrlString.Length, base64Url, 0);

            var size = WebEncoders.GetArraySizeRequiredToDecode(base64Url.Length);
            var bytes = new byte[size];

            // Act
            var status = WebEncoders.Base64UrlDecode(base64Url.AsSpan(0, base64Url.Length / 2), bytes.AsSpan(), out int consumed, out int written1, isFinalBlock: false);
            Assert.Equal(OperationStatus.NeedMoreData, status);
            status = WebEncoders.Base64UrlDecode(base64Url.AsSpan(consumed), bytes.AsSpan(written1), out consumed, out int written2, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);

            // Assert
            var expected = data;
            var actual = bytes.AsSpan(0, written1 + written2);
            Assert.Equal(expected.Length, actual.Length);
#if !NET461
            Assert.True(expected.AsSpan().SequenceEqual(actual));
#else
            Assert.Equal(string.Join(",", expected), string.Join(",", actual.ToArray()));
#endif
        }

        [Fact]
        public void Base64UrlEncode_BufferChain()
        {
            // Arrange
            var data = new byte[200];
            var rnd = new Random(0);
            rnd.NextBytes(data);

            var size = WebEncoders.GetArraySizeRequiredToEncode(data.Length);
            var base64Url = new byte[size];

            // Act
            var status = WebEncoders.Base64UrlEncode(data.AsSpan(0, data.Length / 2), base64Url.AsSpan(), out int consumed, out int written1, isFinalBlock: false);
            Assert.Equal(OperationStatus.NeedMoreData, status);
            status = WebEncoders.Base64UrlEncode(data.AsSpan(consumed), base64Url.AsSpan(written1), out consumed, out int written2, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);

            // Assert
            var expected = WebEncoders.Base64UrlEncode(data);
            Assert.Equal(expected.Length, written1 + written2);
            var chars = new char[expected.Length];
            Encoding.ASCII.GetChars(base64Url, 0, written1 + written2, chars, 0);
#if NETCOREAPP2_1
            var actual = new String(chars);
#else
            var actual = new String(chars.ToArray());
#endif
            Assert.Equal(expected, actual);
        }
    }
}
