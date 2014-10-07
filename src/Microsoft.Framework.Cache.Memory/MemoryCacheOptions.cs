// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Cache.Memory.Infrastructure;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.Cache.Memory
{
    public class MemoryCacheOptions : IOptionsAccessor<MemoryCacheOptions>
    {
        public ISystemClock Clock { get; set; }

        public bool ListenForMemoryPressure { get; set; } = true;

        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

        MemoryCacheOptions IOptionsAccessor<MemoryCacheOptions>.Options
        {
            get { return this; }
        }

        MemoryCacheOptions IOptionsAccessor<MemoryCacheOptions>.GetNamedOptions(string name)
        {
            return this;
        }
    }
}