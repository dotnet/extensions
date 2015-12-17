// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Extensions.ObjectPool
{
    public class StringBuilderPooledObjectPolicy : IPooledObjectPolicy<StringBuilder>
    {
        public int InitialCapacity { get; set; } = 100;

        public int MaximumRetainedCapacity { get; set; } = 4 * 1024;

        public StringBuilder Create()
        {
            return new StringBuilder(InitialCapacity);
        }

        public bool Return(StringBuilder obj)
        {
            if (obj.Capacity > MaximumRetainedCapacity)
            {
                // Too big. Discard this one.
                return false;
            }

            obj.Clear();
            return true;
        }
    }
}
