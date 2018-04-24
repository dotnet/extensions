// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if RELEASE

using System;
using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public partial class MemoryPoolTests
    {
        [Fact]
        public void DoubleDisposeWorks()
        {
            var memoryPool = new SlabMemoryPool();
            memoryPool.Dispose();
            memoryPool.Dispose();
        }

        [Fact]
        public void DisposeWithActiveBlocksWorks()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            memoryPool.Dispose();
        }
    }
}

#endif