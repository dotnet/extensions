// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

using System;
using System.Buffers;
using System.Runtime.InteropServices;
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
            ExpectDisposeException(memoryPool);

            var exception = Assert.Throws<InvalidOperationException>(() => block.Dispose());
            Assert.Equal("Block is being returned to disposed pool", exception.Message);
        }

        [Fact]
        public void GetMemoryOfDisposedThrows()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();

            ExpectDisposeException(memoryPool);

            var exception = Assert.Throws<ObjectDisposedException>(() => block.Memory);
            Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);
        }

        [Fact]
        public void GetMemoryPinOfDisposedThrows()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            var memory = block.Memory;

            ExpectDisposeException(memoryPool);

            var exception = Assert.Throws<ObjectDisposedException>(() => memory.Pin());
            Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);
        }

        [Fact]
        public void GetMemorySpanOfDisposedThrows()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            var memory = block.Memory;

            ExpectDisposeException(memoryPool);

            var threw = false;
            try
            {
                _ = memory.Span;
            }
            catch (ObjectDisposedException ode)
            {
                threw = true;
                Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", ode.Message);
            }
            Assert.True(threw);
        }

        [Fact]
        public void GetMemoryTryGetArrayOfDisposedThrows()
        {
            var memoryPool = new SlabMemoryPool();
            var block = memoryPool.Rent();
            var memory = block.Memory;

            ExpectDisposeException(memoryPool);

            var exception = Assert.Throws<ObjectDisposedException>(() => MemoryMarshal.TryGetArray<byte>(memory, out _));
            Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);
        }

        private static void ExpectDisposeException(SlabMemoryPool memoryPool)
        {
            var exception = Assert.Throws<InvalidOperationException>(() => memoryPool.Dispose());
            Assert.Equal("Memory pool with active blocks is being disposed, 30 of 31 returned", exception.Message);
        }
    }
}

#endif