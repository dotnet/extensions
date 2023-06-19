// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Microsoft.Shared.Pools.Test;

public class PoolTests
{
    private static int _fooSequenceNum;

    private sealed class Foo : IResettable
    {
        public int SequenceNum { get; }
        public int ResetCount { get; private set; }
        public volatile bool Busy;

        public Foo()
        {
            SequenceNum = Interlocked.Increment(ref _fooSequenceNum);
        }

        public bool TryReset()
        {
            ResetCount++;
            return true;
        }
    }

    private class FooPolicy : IPooledObjectPolicy<Foo>
    {
        public Foo Create()
        {
            return new Foo();
        }

        public bool Return(Foo obj)
        {
            if (obj.SequenceNum % 2 == 0)
            {
                return obj.TryReset();
            }

            return false;
        }
    }

    [Fact]
    public void Basic()
    {
        const int Capacity = 200;
        const int Extra = 50;

        _fooSequenceNum = -1;
        var pool = PoolFactory.CreatePool<Foo>(Capacity);
        var set = new HashSet<Foo>();

        for (int i = 0; i < Capacity + Extra; i++)
        {
            set.Add(pool.Get());
        }

        foreach (var f in set)
        {
            pool.Return(f);
        }

        // ensure we get back the original objects for anything within the capacity range
        for (int i = 0; i < Capacity; i++)
        {
            var f = pool.Get();
            Assert.True(f.SequenceNum < Capacity, $"{i}");
        }

        // ensure we get back fresh objects for anything beyond the capacity range, demonstrating that the pool only kept capacity's worth of objects.
        for (int i = Capacity; i < Capacity + Extra; i++)
        {
            var f = pool.Get();
            Assert.True(f.SequenceNum >= Capacity + Extra);
        }
    }

    [Fact]
    public void Resettable()
    {
        _fooSequenceNum = -1;
        var pool = PoolFactory.CreateResettingPool<Foo>();

        var f = pool.Get();
        Assert.Equal(0, f.ResetCount);

        pool.Return(f);
        Assert.Equal(1, f.ResetCount);
    }

    [Fact]
    public void RespectPolicy()
    {
        _fooSequenceNum = -1;
        var pool = PoolFactory.CreatePool<Foo>(new FooPolicy());

        var f0 = pool.Get();
        var f1 = pool.Get();
        var f2 = pool.Get();
        var f3 = pool.Get();

        pool.Return(f0);
        pool.Return(f1);
        pool.Return(f2);
        pool.Return(f3);

        Assert.Equal(1, f0.ResetCount);
        Assert.Equal(0, f1.ResetCount);
        Assert.Equal(1, f2.ResetCount);
        Assert.Equal(0, f3.ResetCount);
    }

    [Fact]
    public void SharedStringBuilderPool()
    {
        var pool = PoolFactory.SharedStringBuilderPool;
        var sb = pool.Get();
        Assert.NotNull(sb);
        pool.Return(sb);
    }

    [Fact]
    public void StringBuilderPool()
    {
        var pool = PoolFactory.CreateStringBuilderPool(123, 2048);

        var sb = pool.Get();
        sb.Append('x', 4096);
        pool.Return(sb);

        var sb2 = pool.Get();

        Assert.NotSame(sb, sb2);
    }

    [Fact]
    public void ListPool()
    {
        var pool = PoolFactory.CreateListPool<int>(123);

        var l = pool.Get();
        l.Add(42);
        pool.Return(l);

        var l2 = pool.Get();

        Assert.Same(l, l2);
        Assert.Empty(l2);
    }

    [Fact]
    public void DictionaryPool()
    {
        var pool = PoolFactory.CreateDictionaryPool<string, int>();

        var d = pool.Get();
        d.Add("One", 1);
        pool.Return(d);

        var d2 = pool.Get();

        Assert.Same(d, d2);
        Assert.Empty(d2);
    }

    [Fact]
    public void HashSetPool()
    {
        var pool = PoolFactory.CreateHashSetPool<int>();

        var s = pool.Get();
        s.Add(42);
        pool.Return(s);

        var s2 = pool.Get();

        Assert.Same(s, s2);
        Assert.Empty(s2);
    }

    [Fact]
    public void CancellationTokenSourcePool_NotTriggered()
    {
        var pool = PoolFactory.CreateCancellationTokenSourcePool();

        var s = pool.Get();
        pool.Return(s);
        var s2 = pool.Get();

#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
        // and as such the API is available in .NET 8 onwards.
        Assert.Same(s, s2);
#else
        Assert.NotSame(s, s2);
#endif
    }

    [Fact]
    public void CancellationTokenSourcePool_Triggered()
    {
        var pool = PoolFactory.CreateCancellationTokenSourcePool();

        var s = pool.Get();
        s.Cancel();
        pool.Return(s);

        var s2 = pool.Get();
        Assert.NotSame(s, s2);
    }

    [Fact]
    public void ArgChecks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolFactory.CreatePool<Foo>(0));

        Assert.Throws<ArgumentNullException>(() => PoolFactory.CreatePool<Foo>(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolFactory.CreatePool<Foo>(new FooPolicy(), 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => PoolFactory.CreateResettingPool<Foo>(0));

        Assert.Throws<ArgumentOutOfRangeException>(() => PoolFactory.CreateStringBuilderPool(0, 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoolFactory.CreateStringBuilderPool(200, 0));
    }

    [Fact]
    public async Task Threading()
    {
        const int Capacity = 150;
        const int Delta = 10;

        _fooSequenceNum = -1;
        var pool = PoolFactory.CreatePool<Foo>(maxCapacity: Capacity);

        await Task.WhenAll(new[]
        {
                Task.Run(() => FunWithPools(pool, 1)),
                Task.Run(() => FunWithPools(pool, 2)),
                Task.Run(() => FunWithPools(pool, 3)),
                Task.Run(() => FunWithPools(pool, 4))
            });

        var uniques = new HashSet<Foo>();

        // this loop does two things:
        //
        // #1. It ensures the pool isn't returning any duplicate objects
        // #2. It ensures none of the returned items are busy
        for (int i = 0; i < Capacity + Delta; i++)
        {
            var o = pool.Get();
            Assert.False(o.Busy);
            uniques.Add(o);
        }

        static void FunWithPools(ObjectPool<Foo> pool, int seed)
        {
            var r = new Random(seed);

            var objects = new HashSet<Foo>();
            for (int i = 0; i < 1000; i++)
            {
                // allocate some random number of objects
                for (int j = 0; j < r.Next() % 256; j++)
                {
                    var o = pool.Get();
                    Assert.False(o.Busy);
                    o.Busy = true;
                    objects.Add(o);
                }

                // return some random number of random objects
                for (int j = 0; j < r.Next() % 256; j++)
                {
                    if (objects.Count > 0)
                    {
                        int target = r.Next() % objects.Count;
                        foreach (var o in objects)
                        {
                            if (target == 0)
                            {
                                _ = objects.Remove(o);
                                o.Busy = false;
                                pool.Return(o);
                                break;
                            }

                            target--;
                        }
                    }
                }
            }

            // return remaining objects
            foreach (var o in objects)
            {
                o.Busy = false;
                pool.Return(o);
            }
        }
    }

    [Fact]
    public void NoopPolicy()
    {
        Assert.True(NoopPooledObjectPolicy<object>.Instance.Return(new object()));
    }
}
