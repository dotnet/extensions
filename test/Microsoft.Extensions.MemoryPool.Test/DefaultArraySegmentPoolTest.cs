// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.MemoryPool
{
    public class DefaultArraySegmentPoolTest : IDisposable
    {
        public DefaultArraySegmentPoolTest()
        {
            Pool = new DefaultArraySegmentPool<object>();
            CreatedSegments = new List<LeasedArraySegment<object>>();
        }

        // The pool is explicitly managed by the test class to make it easy to make sure everything is
        // disposed.
        public DefaultArraySegmentPool<object> Pool { get; set; }

        // The segments created by each test are explicitly managed by the test class to make it easy to
        // make sure everything is disposed.
        public List<LeasedArraySegment<object>> CreatedSegments { get; }

        public void Dispose()
        {
            foreach (var segment in CreatedSegments)
            {
                if (segment.Owner != null)
                {
                    segment.Owner.Return(segment);
                }
            }

            if (Pool != null)
            {
                Pool.Dispose();
            }
        }

        public LeasedArraySegment<object> Lease(int size)
        {
            var segment = Pool.Lease(size);
            CreatedSegments.Add(segment);
            return segment;
        }

        [Fact]
        public void Lease_SetsOwner()
        {
            // Arrange & Act
            var segment = Lease(DefaultArraySegmentPool<object>.BlockSize);

            // Assert
            Assert.Same(Pool, segment.Owner);
        }

        public static TheoryData<int> SizesLessThanOrEqualToBlockSize
        {
            get
            {
                return new TheoryData<int>()
                {
                    { 1 },
                    { 10 },
                    { DefaultArraySegmentPool<object>.BlockSize - 1 },
                    { DefaultArraySegmentPool<object>.BlockSize },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SizesLessThanOrEqualToBlockSize))]
        public void Lease_CreatesSegmentMatchingBlockSize(int size)
        {
            // Arrange & Act
            var segment = Lease(size);

            // Assert
            Assert.Equal(DefaultArraySegmentPool<object>.BlockSize, segment.Data.Count);
        }

        public static TheoryData<int> SizesGreaterThanBlockSize
        {
            get
            {
                return new TheoryData<int>()
                {
                    { DefaultArraySegmentPool<object>.BlockSize + 1 },
                    { DefaultArraySegmentPool<object>.BlockSize + 2 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SizesGreaterThanBlockSize))]
        public void Lease_CreatesSegmentAboveBlockSize(int size)
        {
            // Arrange & Act
            var segment = Lease(size);

            // Assert
            Assert.Equal(size, segment.Data.Count);
        }

        [Theory]
        [MemberData(nameof(SizesGreaterThanBlockSize))]
        public void Return_AboveBlockSize_IsNotCached(int size)
        {
            // Arrange
            var segment = Lease(size);

            // Act
            Pool.Return(segment);

            // Assert
            Assert.Null(segment.Data.Array);
            Assert.Null(segment.Owner);
        }

        [Fact]
        public void Return_ToDisposedPool_IsNotCached()
        {
            // Arrange
            var segment = Lease(DefaultArraySegmentPool<object>.BlockSize);

            Pool.Dispose();

            // Act
            Pool.Return(segment);
            Pool = null;

            // Assert
            Assert.Null(segment.Data.Array);
            Assert.Null(segment.Owner);
        }

        [Fact]
        public void Return_ToFullPool_IsNotCached()
        {
            // Arrange
            for (var i = 0; i < DefaultArraySegmentPool<object>.Capacity + 1; i++)
            {
                Lease(DefaultArraySegmentPool<object>.BlockSize);
            }

            for (var i = 0; i < DefaultArraySegmentPool<object>.Capacity; i++)
            {
                Pool.Return(CreatedSegments[i]);
            }

            var segment = CreatedSegments[DefaultArraySegmentPool<object>.Capacity];

            // Act
            Pool.Return(segment);

            // Assert
            Assert.Null(segment.Data.Array);
            Assert.Null(segment.Owner);
        }

        [Theory]
        [MemberData(nameof(SizesGreaterThanBlockSize))]
        public void Lease_AndReturn_AboveBlockSize_IsNotCached(int size)
        {
            // Arrange
            var segment1 = Lease(size);
            Pool.Return(segment1);

            // Act
            var segment2 = Lease(size);

            // Assert
            Assert.NotSame(segment1, segment2);
        }

        [Theory]
        [MemberData(nameof(SizesLessThanOrEqualToBlockSize))]
        public void Lease_AndReturn_MatchingBlockSize_IsCached(int size)
        {
            // Arrange
            var segment1 = Lease(size);
            Pool.Return(segment1);

            // Act
            var segment2 = Lease(size);

            // Assert
            Assert.Same(segment1, segment2);
        }

        [Fact]
        public void Dispose_Disposes_CachedSegments()
        {
            // Arrange
            for (var i = 0; i < DefaultArraySegmentPool<object>.Capacity; i++)
            {
                Pool.Return(Lease(DefaultArraySegmentPool<object>.BlockSize));
            }

            foreach (var segment in CreatedSegments)
            {
                Pool.Return(segment);
            }

            // Act
            Pool.Dispose();
            Pool = null;

            // Assert
            foreach (var segment in CreatedSegments)
            {
                Assert.Null(segment.Data.Array);
                Assert.Null(segment.Owner);
            }
        }
    }
}
