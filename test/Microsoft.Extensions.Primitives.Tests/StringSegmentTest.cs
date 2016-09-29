// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class StringSegmentTest
    {
        [Fact]
        public void StringSegment_StringCtor_AllowsNullBuffers()
        {
            // Arrange & Act
            var segment = new StringSegment(null);

            // Assert
            Assert.False(segment.HasValue);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Length);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("abc", 2, 0)]
        public void StringSegmentConstructor_AllowsEmptyBuffers(string text, int offset, int length)
        {
            // Arrange & Act
            var segment = new StringSegment(text, offset, length);

            // Assert
            Assert.True(segment.HasValue);
            Assert.Equal(offset, segment.Offset);
            Assert.Equal(length, segment.Length);
        }

        [Fact]
        public void StringSegment_StringCtor_InitializesValuesCorrectly()
        {
            // Arrange
            var buffer = "Hello world!";

            // Act
            var segment = new StringSegment(buffer);

            // Assert
            Assert.True(segment.HasValue);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(buffer.Length, segment.Length);
        }

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

        public static TheoryData GetHashCode_ReturnsSameValueForEqualSubstringsData
        {
            get
            {
                return new TheoryData<StringSegment, StringSegment>
                {
                    { default(StringSegment), default(StringSegment) },
                    { default(StringSegment), new StringSegment() },
                    { new StringSegment("Test123", 0, 0), new StringSegment(string.Empty) },
                    { new StringSegment("C`est si bon", 2, 3), new StringSegment("Yesterday", 1, 3) },
                    { new StringSegment("Hello", 1, 4), new StringSegment("Hello world", 1, 4) },
                    { new StringSegment("Hello"), new StringSegment("Hello", 0, 5) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsSameValueForEqualSubstringsData))]
        public void GetHashCode_ReturnsSameValueForEqualSubstrings(StringSegment segment1, StringSegment segment2)
        {
            // Act
            var hashCode1 = segment1.GetHashCode();
            var hashCode2 = segment2.GetHashCode();

            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        public static TheoryData GetHashCode_ReturnsDifferentValuesForInequalSubstringsData
        {
            get
            {
                var testString = "Test123";
                return new TheoryData<StringSegment, StringSegment>
                {
                    { new StringSegment(testString, 0, 1), new StringSegment(string.Empty) },
                    { new StringSegment(testString, 0, 1), new StringSegment(testString, 1, 1) },
                    { new StringSegment(testString, 1, 2), new StringSegment(testString, 1, 3) },
                    { new StringSegment(testString, 0, 4), new StringSegment("TEST123", 0, 4) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_ReturnsDifferentValuesForInequalSubstringsData))]
        public void GetHashCode_ReturnsDifferentValuesForInequalSubstrings(
            StringSegment segment1,
            StringSegment segment2)
        {
            // Act
            var hashCode1 = segment1.GetHashCode();
            var hashCode2 = segment2.GetHashCode();

            // Assert
            Assert.NotEqual(hashCode1, hashCode2);
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

        public static TheoryData<StringSegment, StringComparison> DefaultStringSegmentEqualsStringSegmentData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<StringSegment, StringComparison>()
                {
                    { default(StringSegment), StringComparison.Ordinal},
                    { new StringSegment(), StringComparison.Ordinal},
                };
            }
        }

        [Theory]
        [MemberData(nameof(DefaultStringSegmentEqualsStringSegmentData))]       
        public void DefaultStringSegment_EqualsStringSegment(StringSegment candidate, StringComparison comparison)
        {
            // Arrange
            var segment = default(StringSegment);
           
            // Act
            var result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DefaultStringSegment_DoesNotEqualStringSegment()
        {
            // Arrange
            var segment = default(StringSegment);
            var candidate = new StringSegment("Hello, World!", 1, 4);

            // Act
            var result = segment.Equals(candidate, StringComparison.Ordinal);

            // Assert
            Assert.False(result);
        }

        public static TheoryData<string, StringComparison, bool> DefaultStringSegmentDoesNotEqualStringData
        {
            get
            {
                // candidate / comparer / expected result
                return new TheoryData<string, StringComparison, bool>()
                {
                    { string.Empty, StringComparison.Ordinal, false },
                    { "Hello, World!", StringComparison.Ordinal, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DefaultStringSegmentDoesNotEqualStringData))]       
        public void DefaultStringSegment_DoesNotEqualString(string candidate, StringComparison comparison, bool expectedResult)
        {
            // Arrange
            var segment = default(StringSegment);
            
            // Act
            var result = segment.Equals(candidate, StringComparison.Ordinal);

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
        public void StringSegment_Substring_InvalidOffset()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(-1, 1));
        }

        [Fact]
        public void StringSegment_Substring_InvalidOffsetAndLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Substring(2, 3));
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
        public void StringSegment_Subsegment_InvalidOffset()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(-1, 1));
        }

        [Fact]
        public void StringSegment_Subsegment_InvalidOffsetAndLength()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => segment.Subsegment(2, 3));
        }

        [Fact]
        public void IndexOf_ComputesIndex_RelativeToTheCurrentSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 10);

            // Act
            var result = segment.IndexOf(',');

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void IndexOf_ReturnsMinusOne_IfElementNotInSegment()
        {
            // Arrange
            var segment = new StringSegment("Hello, World!", 1, 3);

            // Act
            var result = segment.IndexOf(',');

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void IndexOf_SkipsANumberOfCaracters_IfStartIsProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            var result = segment.IndexOf('!', 15);

            // Assert
            Assert.Equal(buffer.Length - 4, result);
        }

        [Fact]
        public void IndexOf_SearchOnlyInsideTheRange_IfStartAndCountAreProvided()
        {
            // Arrange
            const string buffer = "Hello, World!, Hello people!";
            var segment = new StringSegment(buffer, 3, buffer.Length - 3);

            // Act
            var result = segment.IndexOf('!', 15, 5);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void Value_DoesNotAllocateANewString_IfTheSegmentContainsTheWholeBuffer()
        {
            // Arrange
            const string buffer = "Hello, World!";
            var segment = new StringSegment(buffer);

            // Act
            var result = segment.Value;

            // Assert
            Assert.Same(buffer, result);
        }

        [Fact]
        public void StringSegment_CreateEmptySegment()
        {
            // Arrange
            var segment = new StringSegment("//", 1, 0);

            // Assert
            Assert.True(segment.HasValue);
        }

        [Theory]
        [InlineData("   value", 0, 8, "value")]
        [InlineData("value   ", 0, 8, "value")]
        [InlineData("\t\tvalue", 0, 7, "value")]
        [InlineData("value\t\t", 0, 7, "value")]
        [InlineData("\t\tvalue \t a", 1, 8, "value")]
        [InlineData("   a     ", 0, 9, "a")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "value")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "Hello")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void Trim_RemovesLeadingAndTrailingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            var actual = segment.Trim();

            // Assert
            Assert.Equal(expected, actual.Value);
        }

        [Theory]
        [InlineData("   value", 0, 8, "value")]
        [InlineData("value   ", 0, 8, "value   ")]
        [InlineData("\t\tvalue", 0, 7, "value")]
        [InlineData("value\t\t", 0, 7, "value\t\t")]
        [InlineData("\t\tvalue \t a", 1, 8, "value \t")]
        [InlineData("   a     ", 0, 9, "a     ")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "value \x0085")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "Hello \u2029\n\t")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void TrimStart_RemovesLeadingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            var actual = segment.TrimStart();

            // Assert
            Assert.Equal(expected, actual.Value);
        }

        [Theory]
        [InlineData("   value", 0, 8, "   value")]
        [InlineData("value   ", 0, 8, "value")]
        [InlineData("\t\tvalue", 0, 7, "\t\tvalue")]
        [InlineData("value\t\t", 0, 7, "value")]
        [InlineData("\t\tvalue \t a", 1, 8, "\tvalue")]
        [InlineData("   a     ", 0, 9, "   a")]
        [InlineData("value\t value  value ", 2, 13, "lue\t value  v")]
        [InlineData("\x0009value \x0085", 0, 8, "\x0009value")]
        [InlineData(" \f\t\u000B\u2028Hello \u2029\n\t ", 1, 13, "\f\t\u000B\u2028Hello")]
        [InlineData("      ", 1, 2, "")]
        [InlineData("\t\t\t", 0, 3, "")]
        [InlineData("\n\n\t\t  \t", 2, 3, "")]
        [InlineData("      ", 1, 0, "")]
        [InlineData("", 0, 0, "")]
        public void TrimEnd_RemovesTrailingWhitespaces(string value, int start, int length, string expected)
        {
            // Arrange
            var segment = new StringSegment(value, start, length);

            // Act
            var actual = segment.TrimEnd();

            // Assert
            Assert.Equal(expected, actual.Value);
        }
    }
}
