// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class WebEncodersTests
    {
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

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 4)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(6, 8)]
        [InlineData(7, 8)]
        public void GetArraySizeRequiredToDecode(int inputLength, int expectedPadding)
        {
            var result = WebEncoders.GetArraySizeRequiredToDecode(inputLength);

            Assert.Equal(expectedPadding, result);
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
        //[InlineData(-1)]
        //[InlineData(-2)]
        public void GetArraySizeRequiredToDecode_MalformedInputLength(int inputLength)
        {
            Assert.Throws<FormatException>(() =>
            {
                var retVal = WebEncoders.GetArraySizeRequiredToDecode(inputLength);
            });
        }

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

        // Taken from https://github.com/aspnet/HttpAbstractions/pull/926
        [Theory]
        [InlineData("_", "/===")]
        [InlineData("-", "+===")]
        [InlineData("a-b-c", "a+b+c===")]
        [InlineData("a_b_c_d", "a/b/c/d=")]
        [InlineData("a-b_c", "a+b/c===")]
        [InlineData("a-b_c-d", "a+b/c+d=")]
        [InlineData("abcd", "abcd")]
        public void UrlDecode_ReturnsValid_Base64String(string text, string expectedValue)
        {
#if NETCOREAPP2_1
            // Arrange
            Span<byte> bytes = stackalloc byte[text.Length];
            WebEncoders.EncodingHelper.GetBytes(text.AsSpan(), bytes);
            Span<byte> expected = stackalloc byte[expectedValue.Length];
            WebEncoders.EncodingHelper.GetBytes(expectedValue.AsSpan(), expected);
            Span<byte> result = stackalloc byte[expectedValue.Length];

            // Act
            WebEncoders.EncodingHelper.UrlDecode(bytes, result);

            // Assert
            Assert.True(expected.SequenceEqual(result));
#else
            // Arrange
            var buffer = new char[expectedValue.Length];

            // Act
            WebEncoders.EncodingHelper.UrlDecode(text.AsSpan(), buffer);

            // Assert
            for (var i = 0; i < expectedValue.Length; i++)
            {
                Assert.Equal(expectedValue[i], buffer[i]);
            }
#endif
        }

        // Taken from https://github.com/aspnet/HttpAbstractions/pull/926
        [Theory]
        [InlineData("", "")]
        [InlineData("+", "-")]
        [InlineData("/", "_")]
        [InlineData("=", "")]
        [InlineData("==", "")]
        [InlineData("a+b+c+==", "a-b-c-")]
        [InlineData("a/b/c==", "a_b_c")]
        [InlineData("a+b/c==", "a-b_c")]
        [InlineData("a+b/c", "a-b_c")]
        [InlineData("abcd", "abcd")]
        public void UrlEncode_Replaces_UrlEncodableCharacters(string base64EncodedValue, string expectedValue)
        {
#if NETCOREAPP2_1
            // Arrange
            Span<byte> bytes = stackalloc byte[base64EncodedValue.Length];
            WebEncoders.EncodingHelper.GetBytes(base64EncodedValue.AsSpan(), bytes);
            Span<byte> expected = stackalloc byte[expectedValue.Length];
            WebEncoders.EncodingHelper.GetBytes(expectedValue.AsSpan(), expected);

            // Act
            var result = WebEncoders.EncodingHelper.UrlEncode(bytes);

            // Assert
            Assert.True(expected.SequenceEqual(bytes.Slice(0, result)));
#else
            // Arrange
            var buffer = base64EncodedValue.ToCharArray();

            // Act
            var result = WebEncoders.EncodingHelper.UrlEncode(buffer);

            // Assert
            Assert.Equal(expectedValue.Length, result);
            for (var i = 0; i < result; i++)
            {
                Assert.Equal(expectedValue[i], buffer[i]);
            }
#endif
        }
    }
}
