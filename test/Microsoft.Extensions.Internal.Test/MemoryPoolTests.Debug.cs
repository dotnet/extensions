// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

using System;
using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public partial class MemoryPoolTests
    {
        [Fact]
        public void DoubleDisposeThrows()
        {
            var memoryPool = new SlabMemoryPool();
            memoryPool.Dispose();
            var exception = Assert.Throws<InvalidOperationException>(() => memoryPool.Dispose());
            Assert.Equal("Object is being disposed twice", exception.Message);
        }

        [Fact]
        public void DisposeWithActiveBlocksThrows()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            var exception = Assert.Throws<InvalidOperationException>(() => memoryPool.Dispose());
            Assert.Equal("Memory pool with active blocks is being disposed, 30 of 31 returned", exception.Message);

            exception = Assert.Throws<InvalidOperationException>(() => block.Dispose());
            Assert.Equal("Block is being returned to disposed pool", exception.Message);
        }
    }
}

#endif