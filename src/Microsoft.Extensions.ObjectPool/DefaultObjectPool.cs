// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultObjectPool<T> : ObjectPool<T> where T : class
    {
        private readonly T[] _items;
        private readonly IPooledObjectPolicy<T> _policy;

        public DefaultObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {
        }

        public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            _policy = policy;
            _items = new T[maximumRetained];
        }

        public override T Get()
        {
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item != null && Interlocked.CompareExchange(ref _items[i], null, item) == item)
                {
                    return item;
                }
            }

            return _policy.Create();
        }

        public override void Return(T obj)
        {
            if (!_policy.Return(obj))
            {
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    _items[i] = obj;
                    return;
                }
            }
        }
    }
}
