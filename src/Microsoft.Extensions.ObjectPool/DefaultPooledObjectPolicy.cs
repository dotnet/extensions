// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultPooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : class, new()
    {
        public T Create()
        {
            return new T();
        }

        public bool Return(T obj)
        {
            return true;
        }
    }
}
