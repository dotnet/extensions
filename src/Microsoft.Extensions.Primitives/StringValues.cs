// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Represents zero/null, one, or many strings in an efficient way.
    /// </summary>
    public class StringValues : IList<string>, IReadOnlyList<string>, IEquatable<StringValues>, IEquatable<string>, IEquatable<string[]>
    {
        private static readonly string[] EmptyArray = new string[0];
        public static readonly StringValues Empty = new StringValues(EmptyArray);

        private readonly string _value;
        private readonly string[] _values;
        private readonly Encoding _encoding;
        private readonly byte[] _bytes;

        public StringValues(string value)
        {
            _value = value;
        }

        public StringValues(string[] values)
        {
            _values = values;
        }

        private StringValues(string value, Encoding encoding)
        {
            _value = value;
            _encoding = encoding;
            _bytes = _encoding.GetBytes(value);
        }

        /// <summary>
        /// Creates a new instance of <see cref="StringValues"/> with one string and compute its encoded bytes with the specified Encoding.
        /// </summary>
        /// <param name="value">The string to be encoded.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to be use when computing pre-encoded bytes.</param>
        /// <returns>A <see cref="StringValues"/> which contains the pre-encoded bytes.</returns>
        public static StringValues CreatePreEncoded(string value, Encoding encoding)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return new StringValues(value, encoding);
        }

        /// <summary>
        /// Try to retrieve the pre-encoded bytes of the <see cref="StringValues"/>. The return value indicates whether pre-encoded bytes exist.
        /// </summary>
        /// <param name="bytes">If successful, <paramref name="bytes"/> contains the pre-encoded bytes of the <see cref="StringValues"/>.</param>
        /// <param name="encoding">If successful, <paramref name="encoding"/> contains the <see cref="Encoding"/> used to compute the pre-encoded bytes.</param>
        /// <returns><c>true</c> if <see cref="StringValues"/> contains pre-encoded bytes, otherwise <c>false</c>.</returns>
        public bool TryGetPreEncoded(out byte[] bytes, out Encoding encoding)
        {
            bytes = _bytes;
            encoding = _encoding;
            return _bytes != null;
        }

        public static implicit operator StringValues(string value)
        {
            return new StringValues(value);
        }

        public static implicit operator StringValues(string[] values)
        {
            return new StringValues(values);
        }

        public static implicit operator string (StringValues values)
        {
            return values.GetStringValue();
        }

        public static implicit operator string[] (StringValues value)
        {
            return value.GetArrayValue();
        }

        public int Count => _value != null ? 1 : (_values?.Length ?? 0);

        bool ICollection<string>.IsReadOnly
        {
            get { return true; }
        }

        string IList<string>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        public string this[int index]
        {
            get
            {
                if (_values != null)
                {
                    return _values[index]; // may throw
                }
                if (index == 0 && _value != null)
                {
                    return _value;
                }
                return EmptyArray[0]; // throws
            }
        }

        public override string ToString()
        {
            return GetStringValue() ?? string.Empty;
        }

        private string GetStringValue()
        {
            if (_values == null)
            {
                return _value;
            }
            switch (_values.Length)
            {
                case 0: return null;
                case 1: return _values[0];
                default: return string.Join(",", _values);
            }
        }

        public string[] ToArray()
        {
            return GetArrayValue() ?? EmptyArray;
        }

        private string[] GetArrayValue()
        {
            if (_value != null)
            {
                return new[] { _value };
            }
            return _values;
        }

        int IList<string>.IndexOf(string item)
        {
            return IndexOf(item);
        }

        private int IndexOf(string item)
        {
            if (_values != null)
            {
                var values = _values;
                for (int i = 0; i < values.Length; i++)
                {
                    if (string.Equals(values[i], item, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
                return -1;
            }

            if (_value != null)
            {
                return string.Equals(_value, item, StringComparison.Ordinal) ? 0 : -1;
            }

            return -1;
        }

        bool ICollection<string>.Contains(string item)
        {
            return IndexOf(item) >= 0;
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        private void CopyTo(string[] array, int arrayIndex)
        {
            if (_values != null)
            {
                Array.Copy(_values, 0, array, arrayIndex, _values.Length);
                return;
            }

            if (_value != null)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }
                if (arrayIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                }
                if (array.Length - arrayIndex < 1)
                {
                    throw new ArgumentException(
                        $"'{nameof(array)}' is not long enough to copy all the items in the collection. Check '{nameof(arrayIndex)}' and '{nameof(array)}' length.");
                }

                array[arrayIndex] = _value;
            }
        }

        void ICollection<string>.Add(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<string>.Remove(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<string>.Clear()
        {
            throw new NotSupportedException();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool IsNullOrEmpty(StringValues value)
        {
            if (value._values == null)
            {
                return string.IsNullOrEmpty(value._value);
            }
            switch (value._values.Length)
            {
                case 0: return true;
                case 1: return string.IsNullOrEmpty(value._values[0]);
                default: return false;
            }
        }

        public static StringValues Concat(StringValues values1, StringValues values2)
        {
            var count1 = values1.Count;
            var count2 = values2.Count;

            if (count1 == 0)
            {
                return values2;
            }

            if (count2 == 0)
            {
                return values1;
            }

            var combined = new string[count1 + count2];
            values1.CopyTo(combined, 0);
            values2.CopyTo(combined, count1);
            return new StringValues(combined);
        }

        public static bool Equals(StringValues left, StringValues right)
        {
            var count = left.Count;

            if (count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(StringValues left, StringValues right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StringValues left, StringValues right)
        {
            return !Equals(left, right);
        }

        public bool Equals(StringValues other)
        {
            return Equals(this, other);
        }

        public static bool Equals(string left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string right)
        {
            return Equals(left, new StringValues(right));
        }

        public bool Equals(string other)
        {
            return Equals(this, new StringValues(other));
        }

        public static bool Equals(string[] left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string[] right)
        {
            return Equals(left, new StringValues(right));
        }

        public bool Equals(string[] other)
        {
            return Equals(this, new StringValues(other));
        }

        public static bool operator ==(StringValues left, string right)
        {
            return Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string right)
        {
            return !Equals(left, new StringValues(right));
        }

        public static bool operator ==(string left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool operator !=(string left, StringValues right)
        {
            return !Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, string[] right)
        {
            return Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string[] right)
        {
            return !Equals(left, new StringValues(right));
        }

        public static bool operator ==(string[] left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool operator !=(string[] left, StringValues right)
        {
            return !Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, object right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringValues left, object right)
        {
            return !left.Equals(right);
        }
        public static bool operator ==(object left, StringValues right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(object left, StringValues right)
        {
            return !right.Equals(left);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return Equals(this, StringValues.Empty);
            }

            if (obj is string)
            {
                return Equals(this, (string)obj);
            }
            
            if (obj is string[])
            {
                return Equals(this, (string[])obj);
            }

            if (obj is StringValues)
            {
                return Equals(this, (StringValues)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (_values == null)
            {
                return _value == null ? 0 : _value.GetHashCode();
            }

            var hcc = new HashCodeCombiner();
            for (var i = 0; i < _values.Length; i++)
            {
                hcc.Add(_values[i]);
            }
            return hcc.CombinedHash;
        }

        public struct Enumerator : IEnumerator<string>
        {
            private readonly string[] _values;
            private string _current;
            private int _index;

            public Enumerator(StringValues values)
            {
                _values = values._values;
                _current = values._value;
                _index = 0;
            }

            public bool MoveNext()
            {
                if (_index < 0)
                {
                    return false;
                }

                if (_values != null)
                {
                    if (_index < _values.Length)
                    {
                        _current = _values[_index];
                        _index++;
                        return true;
                    }

                    _index = -1;
                    return false;
                }

                _index = -1; // sentinel value
                return _current != null;
            }

            public string Current => _current;

            object IEnumerator.Current => _current;

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
