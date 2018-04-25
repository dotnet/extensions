// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public partial class MemoryPoolTests
    {
        [Fact]
        public void CanDisposeAfterCreation()
        {
            var memoryPool = new SlabMemoryPool();
            memoryPool.Dispose();
        }

        [Fact]
        public void CanDisposeAfterReturningBlock()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            block.Dispose();
            memoryPool.Dispose();
        }

        [Fact]
        public void LeasingFromDisposedPoolThrows()
        {
            var memoryPool = new SlabMemoryPool();
            memoryPool.Dispose();

            var exception = Assert.Throws<ObjectDisposedException>(() => memoryPool.Rent());
            Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPool'.", exception.Message);
        }
    }
}
