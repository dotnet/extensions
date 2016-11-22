// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// An optimized representation of a substring.
    /// </summary>
    public struct StringSegment : IEquatable<StringSegment>, IEquatable<string>
    {
        /// <summary>
        /// Initializes an instance of the <see cref="StringSegment"/> struct.
        /// </summary>
        /// <param name="buffer">
        /// The original <see cref="string"/>. The <see cref="StringSegment"/> includes the whole <see cref="string"/>.
        /// </param>
        public StringSegment(string buffer)
        {
            Buffer = buffer;
            Offset = 0;
            Length = buffer != null ? buffer.Length : 0;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="StringSegment"/> struct.
        /// </summary>
        /// <param name="buffer">The original <see cref="string"/> used as buffer.</param>
        /// <param name="offset">The offset of the segment within the <paramref name="buffer"/>.</param>
        /// <param name="length">The length of the segment.</param>
        public StringSegment(string buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (offset > buffer.Length - length)
            {
                throw new ArgumentException(Resources.Argument_InvalidOffsetLength);
            }

            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// Gets the <see cref="string"/> buffer for this <see cref="StringSegment"/>.
        /// </summary>
        public string Buffer { get; }

        /// <summary>
        /// Gets the offset within the buffer for this <see cref="StringSegment"/>.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the length of this <see cref="StringSegment"/>.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the value of this segment as a <see cref="string"/>.
        /// </summary>
        public string Value
        {
            get
            {
                if (!HasValue)
                {
                    return null;
                }
                else
                {
                    return Buffer.Substring(Offset, Length);
                }
            }
        }

        /// <summary>
        /// Gets whether or not this <see cref="StringSegment"/> contains a valid value.
        /// </summary>
        public bool HasValue
        {
            get { return Buffer != null; }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is StringSegment && Equals((StringSegment)obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><code>true</code> if the current object is equal to the other parameter; otherwise, <code>false</code>.</returns>
        public bool Equals(StringSegment other)
        {
            return Equals(other, StringComparison.Ordinal);
        }


        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
        /// <returns><code>true</code> if the current object is equal to the other parameter; otherwise, <code>false</code>.</returns>
        public bool Equals(StringSegment other, StringComparison comparisonType)
        {
            int textLength = other.Length;
            if (Length != textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset, other.Buffer, other.Offset, textLength, comparisonType) == 0;
        }

        /// <summary>
        /// Checks if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="text">The <see cref="string"/> to compare with the current <see cref="StringSegment"/>.</param>
        /// <returns><code>true</code> if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
        public bool Equals(string text)
        {
            return Equals(text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="text">The <see cref="string"/> to compare with the current <see cref="StringSegment"/>.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
        /// <returns><code>true</code> if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
        public bool Equals(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || Length != textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset, text, 0, textLength, comparisonType) == 0;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This GetHashCode is expensive since it allocates on every call.
        /// However this is required to ensure we retain any behavior (such as hash code randomization) that
        /// string.GetHashCode has.
        /// </remarks>
        public override int GetHashCode()
        {
            if (!HasValue)
            {
                return 0;
            }
            else
            {
                return Value.GetHashCode();
            }
        }

        /// <summary>
        /// Checks if two specified <see cref="StringSegment"/> have the same value.
        /// </summary>
        /// <param name="left">The first <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
        /// <param name="right">The second <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
        /// <returns><code>true</code> if the value of <paramref name="left"/> is the same as the value of <paramref name="right"/>; otherwise, <code>false</code>.</returns>
        public static bool operator ==(StringSegment left, StringSegment right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks if two specified <see cref="StringSegment"/> have different values.
        /// </summary>
        /// <param name="left">The first <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
        /// <param name="right">The second <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
        /// <returns><code>true</code> if the value of <paramref name="left"/> is different from the value of <paramref name="right"/>; otherwise, <code>false</code>.</returns>
        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks if the beginning of this <see cref="StringSegment"/> matches the specified <see cref="string"/> when compared using the specified <paramref name="comparisonType"/>.
        /// </summary>
        /// <param name="text">The <see cref="string"/>to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
        /// <returns><code>true</code> if <paramref name="text"/> matches the beginning of this <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
        public bool StartsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var textLength = text.Length;
            if (!HasValue || Length < textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset, text, 0, textLength, comparisonType) == 0;
        }

        /// <summary>
        /// Checks if the end of this <see cref="StringSegment"/> matches the specified <see cref="string"/> when compared using the specified <paramref name="comparisonType"/>.
        /// </summary>
        /// <param name="text">The <see cref="string"/>to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
        /// <returns><code>true</code> if <paramref name="text"/> matches the end of this <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
        public bool EndsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var textLength = text.Length;
            if (!HasValue || Length < textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset + Length - textLength, text, 0, textLength, comparisonType) == 0;
        }

        /// <summary>
        /// Retrieves a substring from this <see cref="StringSegment"/>.
        /// The substring starts at the position specified by <paramref name="offset"/> and has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The zero-based starting character position of a substring in this <see cref="StringSegment"/>.</param>
        /// <param name="length">The number of characters in the substring.</param>
        /// <returns>A <see cref="string"/> that is equivalent to the substring of length <paramref name="length"/> that begins at <paramref name="offset"/> in this <see cref="StringSegment"/></returns>
        public string Substring(int offset, int length)
        {
            if (!HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset < 0 || offset + length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0 || Offset + offset + length > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return Buffer.Substring(Offset + offset, length);
        }

        /// <summary>
        /// Retrieves a <see cref="StringSegment"/> that represents a substring from this <see cref="StringSegment"/>.
        /// The <see cref="StringSegment"/> starts at the position specified by <paramref name="offset"/> and has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The zero-based starting character position of a substring in this <see cref="StringSegment"/>.</param>
        /// <param name="length">The number of characters in the substring.</param>
        /// <returns>A <see cref="StringSegment"/> that is equivalent to the substring of length <paramref name="length"/> that begins at <paramref name="offset"/> in this <see cref="StringSegment"/></returns>
        public StringSegment Subsegment(int offset, int length)
        {
            if (!HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset < 0 || offset + length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0 || Offset + offset + length > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new StringSegment(Buffer, Offset + offset, length);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
        /// The search starts at <paramref name="start"/> and examines a specified number of <paramref name="count"/> character positions.
        /// </summary>
        /// <param name="c">The Unicode character to seek.</param>
        /// <param name="start">The zero-based index position at which the search starts. </param>
        /// <param name="count">The number of characters to examine.</param>
        /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
        public int IndexOf(char c, int start, int count)
        {
            if (start < 0 || Offset + start > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (count < 0 || Offset + start + count > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            var index = Buffer.IndexOf(c, start + Offset, count);
            if (index != -1)
            {
                return index - Offset;
            }
            else
            {
                return index;
            }
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
        /// The search starts at <paramref name="start"/>.
        /// </summary>
        /// <param name="c">The Unicode character to seek.</param>
        /// <param name="start">The zero-based index position at which the search starts. </param>
        /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
        public int IndexOf(char c, int start)
        {
            return IndexOf(c, start, Length - start);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="c">The Unicode character to seek.</param>
        /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
        public int IndexOf(char c)
        {
            return IndexOf(c, 0, Length);
        }

        /// <summary>
        /// Removes all leading and trailing whitespaces.
        /// </summary>
        /// <returns>The trimmed <see cref="StringSegment"/>.</returns>
        public StringSegment Trim()
        {
            return TrimStart().TrimEnd();
        }

        /// <summary>
        /// Removes all leading whitespaces.
        /// </summary>
        /// <returns>The trimmed <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart()
        {
            var trimmedStart = Offset;
            while (trimmedStart < Offset + Length)
            {
                if (!char.IsWhiteSpace(Buffer, trimmedStart))
                {
                    break;
                }

                trimmedStart++;
            }

            return new StringSegment(Buffer, trimmedStart, Offset + Length - trimmedStart);
        }

        /// <summary>
        /// Removes all trailing whitespaces.
        /// </summary>
        /// <returns>The trimmed <see cref="StringSegment"/>.</returns>
        public StringSegment TrimEnd()
        {
            var trimmedEnd = Offset + Length - 1;
            while (trimmedEnd >= Offset)
            {
                if (!char.IsWhiteSpace(Buffer, trimmedEnd))
                {
                    break;
                }

                trimmedEnd--;
            }

            return new StringSegment(Buffer, Offset, trimmedEnd - Offset + 1);
        }

        /// <summary>
        /// Returns the <see cref="string"/> represented by this <see cref="StringSegment"/> or <code>String.Empty</code> if the <see cref="StringSegment"/> does not contain a value.
        /// </summary>
        /// <returns>The <see cref="string"/> represented by this <see cref="StringSegment"/> or <code>String.Empty</code> if the <see cref="StringSegment"/> does not contain a value.</returns>
        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}