// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.MemoryPool
{
    public interface IArraySegmentPool<T>
    {
        LeasedArraySegment<T> Lease(int size);

        void Return(LeasedArraySegment<T> buffer);
    }
}
