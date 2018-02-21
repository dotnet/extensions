// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.ObjectPool
{
    public class StringBuilderPoolBenchmark
    {
        // Based on the default pool size 
        private readonly int PoolSize = Environment.ProcessorCount * 2;

        private readonly ObjectPool<StringBuilder> _emptyPool;
        private readonly ObjectPool<StringBuilder> _halfFullPool;
        private readonly ObjectPool<StringBuilder> _fullPool;

        public StringBuilderPoolBenchmark()
        {
            var policy = new StringBuilderPooledObjectPolicy();

            _emptyPool = new DefaultObjectPool<StringBuilder>(policy, PoolSize);
            _halfFullPool = new DefaultObjectPool<StringBuilder>(policy, PoolSize);
            _fullPool = new DefaultObjectPool<StringBuilder>(policy, PoolSize);

            // Empty Pool needs no initialization, it's already empty

            // Half Full Pool should have items in half of its slots
            StringBuilder item = null;
            for (var i = 0; i < PoolSize; i++)
            {
                item = _halfFullPool.Get();
            }

            for (var i = 0; i < PoolSize / 2; i++)
            {
                _halfFullPool.Return(item);
            }

            // Full Pool should have items in all of it's slots
            for (var i = 0; i < PoolSize; i++)
            {
                item = _fullPool.Get();
            }

            for (var i = 0; i < PoolSize; i++)
            {
                _fullPool.Return(item);
            }
        }

        // This case tests the path that causes a new object to be created.
        [Benchmark]
        public void Get_WithEmptyPool()
        {
            var pool = _emptyPool;

            for (var i = 0; i < PoolSize; i++)
            {
                var item = pool.Get();
            }
        }

        // This case highlights the expected main usage of the object pool
        [Benchmark]
        public void GetAndReturn_WithPoolHalfFull()
        {
            var pool = _halfFullPool;

            StringBuilder item = null;
            for (var i = 0; i < PoolSize / 2; i++)
            {
                item = pool.Get();
            }

            for (var i = 0; i < PoolSize; i++)
            {
                pool.Return(item);
            }

            for (var i = 0; i < PoolSize / 2; i++)
            {
                item = pool.Get();
            }
        }

        // This case highlights a 'first item' optimization by getting and returning only one item from the pool
        [Benchmark]
        public void GetAndReturn_WithFullPool()
        {
            var pool = _fullPool;

            for (var i = 0; i < PoolSize; i++)
            {
                var item = pool.Get();
                pool.Return(item);
            }
        }
    }
}
