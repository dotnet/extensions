// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultObjectPool<T> : ObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _items;
        private readonly int _maximumRetained;
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
            _maximumRetained = maximumRetained;
            _items = new ConcurrentQueue<T>();
        }

        public override T Get()
        {
            T item;
            if (_items.TryDequeue(out item))
            {
                return item;
            }

            return _policy.Create();
        }

        public override void Return(T obj)
        {
            if (!_policy.Return(obj))
            {
                return;
            }

            if (_items.Count < _maximumRetained)
            {
                _items.Enqueue(obj);
            }
        }
    }
}
