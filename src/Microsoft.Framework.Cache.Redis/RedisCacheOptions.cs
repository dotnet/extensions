// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.Cache.Redis
{
    public class RedisCacheOptions : IOptions<RedisCacheOptions>
    {
        public string Configuration { get; set; }
        
        public string InstanceName { get; set; }

        RedisCacheOptions IOptions<RedisCacheOptions>.Options
        {
            get { return this; }
        }

        RedisCacheOptions IOptions<RedisCacheOptions>.GetNamedOptions(string name)
        {
            return this;
        }
    }
}