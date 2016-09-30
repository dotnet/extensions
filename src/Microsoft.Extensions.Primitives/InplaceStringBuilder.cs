// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Primitives
{
    public struct InplaceStringBuilder
    {
        private int _capacity;
        private int _offset;
        private bool _writing;
        private string _value;

        public InplaceStringBuilder(int capacity) : this()
        {
            _capacity = capacity;
        }

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (_writing)
                {
                    throw new InvalidOperationException("Cannot change capacity after write started.");
                }
                _capacity = value;
            }
        }

        public unsafe void Append(string s)
        {
            EnsureCapacity(s.Length);
            fixed (char* value = _value)
            fixed (char* pDomainToken = s)
            {
                //TODO: Use CopyBlockUnaligned when added https://github.com/dotnet/corefx/issues/12243
                Unsafe.CopyBlock(value + _offset, pDomainToken, (uint)s.Length * 2);
                _offset += s.Length;
            }
        }
        public unsafe void Append(char c)
        {
            EnsureCapacity(1);
            fixed (char* value = _value)
            {
                value[_offset++] = c;
            }
        }

        private void EnsureCapacity(int length)
        {
            if (_value == null)
            {
                _writing = true;
                _value = new string('\0', _capacity);
            }
            if (_offset + length > _capacity)
            {
                throw new InvalidOperationException($"Not enough capacity to write '{length}' characters, only '{_capacity - _offset}' left.");
            }
        }

        // Debugger calls ToString so this method should be used to get formatted value
        public string Build()
        {
            if (_offset != _capacity)
            {
                throw new InvalidOperationException($"Entire reserved length was not used. Length: '{_capacity}', written '{_offset}'.");
            }
            return _value;
        }

        public override string ToString()
        {
            // Clone string so we won't be modifying returned string if called before
            // whole value was written
            return new string(_value.ToCharArray());
        }
    }
}
