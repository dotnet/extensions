// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class StringSegmentTest
    {
        [Fact]
        public void StringSegment_Value_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var value = segment.Value;

            // Assert
            Assert.Equal("ello", value);
        }

        [Fact]
        public void StringSegment_Value_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            var value = segment.Value;

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void StringSegment_HasValue_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var hasValue = segment.HasValue;

            // Assert
            Assert.True(hasValue);
        }

        [Fact]
        public void StringSegment_HasValue_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            var hasValue = segment.HasValue;

            // Assert
            Assert.False(hasValue);
        }

        public static TheoryData<string, StringComparison, bool> EndsWithData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<string, StringComparison, bool>()
                {
                    { "Hello", StringComparison.Ordinal, false },
                    { "ello ", StringComparison.Ordinal, false },
                    { "ll", StringComparison.Ordinal, false },
                    { "ello", StringComparison.Ordinal, true },
                    { "llo", StringComparison.Ordinal, true },
                    { "lo", StringComparison.Ordinal, true },
                    { "o", StringComparison.Ordinal, true },
                    { string.Empty, StringComparison.Ordinal, true },
                    { "eLLo", StringComparison.Ordinal, false },
                    { "eLLo", StringComparison.OrdinalIgnoreCase, true },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EndsWithData))]
        public void StringSegment_EndsWith_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.EndsWith(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_EndsWith_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            var result = segment.EndsWith(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        public static TheoryData<string, StringComparison, bool> StartsWithData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<string, StringComparison, bool>()
                {
                    { "Hello", StringComparison.Ordinal, false },
                    { "ello ", StringComparison.Ordinal, false },
                    { "ll", StringComparison.Ordinal, false },
                    { "ello", StringComparison.Ordinal, true },
                    { "ell", StringComparison.Ordinal, true },
                    { "el", StringComparison.Ordinal, true },
                    { "e", StringComparison.Ordinal, true },
                    { string.Empty, StringComparison.Ordinal, true },
                    { "eLLo", StringComparison.Ordinal, false },
                    { "eLLo", StringComparison.OrdinalIgnoreCase, true },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StartsWithData))]
        public void StringSegment_StartsWith_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.StartsWith(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_StartsWith_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            var result = segment.StartsWith(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        public static TheoryData<string, StringComparison, bool> EqualsStringData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<string, StringComparison, bool>()
                {
                    { "eLLo", StringComparison.OrdinalIgnoreCase, true },
                    { "eLLo", StringComparison.Ordinal, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EqualsStringData))]
        public void StringSegment_Equals_String_Valid(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.Equals(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_EqualsString_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act
            var result = segment.Equals(string.Empty, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        public static TheoryData<StringSegment, StringComparison, bool> EqualsStringSegmentData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<StringSegment, StringComparison, bool>()
                {
                    { new StringSegment("Hello, World!", 1, 4), StringComparison.Ordinal, true },
                    { new StringSegment("HELlo, World!", 1, 4), StringComparison.Ordinal, false },
                    { new StringSegment("HELlo, World!", 1, 4), StringComparison.OrdinalIgnoreCase, true },
                    { new StringSegment("ello, World!", 0, 4), StringComparison.Ordinal, true },
                    { new StringSegment("ello, World!", 0, 3), StringComparison.Ordinal, false },
                    { new StringSegment("ello, World!", 1, 3), StringComparison.Ordinal, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EqualsStringSegmentData))]
        public void StringSegment_Equals_String_Valid(StringSegment candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.Equals(candidate, comparison);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void StringSegment_EqualsStringSegment_Invalid()
        {
            // Arrange
            var segment = new StringSegment();
            var candidate = new StringSegment("Hello, World!", 3, 2);

            // Act
            var result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StringSegment_Substring_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.Substring(offset: 1, length: 2);

            // Assert
            Assert.Equal("ll", result);
        }

        [Fact]
        public void StringSegment_Substring_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(0, 0));
        }

        [Fact]
        public void StringSegment_Subsegment_Valid()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.Subsegment(offset: 1, length: 2);

            // Assert
            Assert.Equal(new StringSegment("Hello, World!", 2, 2), result);
            Assert.Equal("ll", result.Value);
        }

        [Fact]
        public void StringSegment_Subsegment_Invalid()
        {
            // Arrange
            var segment = new StringSegment();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(0, 0));
        }

        [Fact]
        public void StringSegment_CreateEmptySegment()
        {
            // Arrange
            var segment = new StringSegment("//", 1, 0);

            // Assert
            Assert.True(segment.HasValue);
        }
    }
}
