// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal.VSRC1;

namespace Microsoft.Extensions.Primitives.VSRC1
{
    public struct StringSegment : IEquatable<StringSegment>, IEquatable<string>
    {
        public StringSegment(string buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0 || offset + length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        public string Buffer { get; }

        public int Offset { get; }

        public int Length { get; }

        public string Value
        {
            get { return HasValue ? Buffer.Substring(Offset, Length) : null; }
        }

        public bool HasValue
        {
            get { return Buffer != null; }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is StringSegment && Equals((StringSegment)obj);
        }

        public bool Equals(StringSegment other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public bool Equals(StringSegment other, StringComparison comparisonType)
        {
            int textLength = other.Length;
            if (!HasValue || Length != textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset, other.Buffer, other.Offset, textLength, comparisonType) == 0;
        }

        public bool Equals(string text)
        {
            return Equals(text, StringComparison.Ordinal);
        }

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

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(Value);
            hash.Add(Offset);
            hash.Add(Length);
            return hash;
        }

        public static bool operator ==(StringSegment left, StringSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !left.Equals(right);
        }

        public bool StartsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || Length < textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset, text, 0, textLength, comparisonType) == 0;
        }

        public bool EndsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || Length < textLength)
            {
                return false;
            }

            return string.Compare(Buffer, Offset + Length - textLength, text, 0, textLength, comparisonType) == 0;
        }

        public string Substring(int offset, int length)
        {
            if (!HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset < 0 || offset > length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0 || Offset + offset + length > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return Buffer.Substring(Offset + offset, length);
        }

        public StringSegment Subsegment(int offset, int length)
        {
            if (!HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset < 0 || offset > length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0 || Offset + offset + length > Buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new StringSegment(Buffer, Offset + offset, length);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}