// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.ObjectPool
{
    public class ObjectPoolBenchmark
    {
        // Based on the default pool size 
        private readonly int PoolSize = Environment.ProcessorCount * 2;

        private readonly ObjectPool<object> _emptyPool;
        private readonly ObjectPool<object> _halfFullPool;
        private readonly ObjectPool<object> _fullPool;

        public ObjectPoolBenchmark()
        {
            var policy = new DefaultPooledObjectPolicy<object>();

            _emptyPool = new DefaultObjectPool<object>(policy, PoolSize);
            _halfFullPool = new DefaultObjectPool<object>(policy, PoolSize);
            _fullPool = new DefaultObjectPool<object>(policy, PoolSize);

            // Empty Pool needs no initialization, it's already empty

            // Half Full Pool should have items in half of its slots
            object item = null;
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

            object item = null;
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