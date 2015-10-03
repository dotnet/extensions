// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.MemoryPool
{
    public class LeasedArraySegment<T>
    {
        public LeasedArraySegment(ArraySegment<T> data, IArraySegmentPool<T> owner)
        {
            Data = data;
            Owner = owner;
        }

        public ArraySegment<T> Data { get; protected set; }

        public IArraySegmentPool<T> Owner { get; protected set; }
    }
}
