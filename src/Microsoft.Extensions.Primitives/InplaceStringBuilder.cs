// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Primitives
{
    [DebuggerDisplay("Value = {_value}")]
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
                //TODO: https://github.com/aspnet/Common/issues/158
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

        public override string ToString()
        {
            if (_offset != _capacity)
            {
                throw new InvalidOperationException($"Entire reserved capacity was not used. Capacity: '{_capacity}', written '{_offset}'.");
            }
            return _value;
        }
    }
}
