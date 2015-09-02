// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.Caching.Memory
{
    public class MemoryCacheOptions : IOptions<MemoryCacheOptions>
    {
        public ISystemClock Clock { get; set; }

        public bool CompactOnMemoryPressure { get; set; } = true;

        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

        MemoryCacheOptions IOptions<MemoryCacheOptions>.Value
        {
            get { return this; }
        }
    }
}