// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.MemoryCache.Infrastructure;

namespace Microsoft.AspNet.MemoryCache
{
    public class TestClock : ISystemClock
    {
        public TestClock()
        {
            UtcNow = new DateTime(2013, 6, 11, 12, 34, 56, 789);
        }

        public DateTime UtcNow { get; set; }

        public void Add(TimeSpan timeSpan)
        {
            UtcNow = UtcNow + timeSpan;
        }
    }
}
