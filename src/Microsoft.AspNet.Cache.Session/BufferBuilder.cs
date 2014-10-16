// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Cache.Session
{
    internal class BufferBuilder
    {
        private IList<byte[]> _segments = new List<byte[]>();

        private int _length;

        public void Add(byte value)
        {
            Add(new byte[] { value });
        }

        public void Add(byte[] segment)
        {
            checked { _length += segment.Length; }
            _segments.Add(segment);
        }

        public byte[] Build()
        {
            var result = new byte[_length];
            int offset = 0;
            foreach (var segment in _segments)
            {
                Buffer.BlockCopy(segment, 0, result, offset, segment.Length);
                offset += segment.Length;
            }
            return result;
        }
    }
}