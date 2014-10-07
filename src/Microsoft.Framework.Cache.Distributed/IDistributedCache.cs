// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.Cache.Distributed
{
    [AssemblyNeutral]
    public interface IDistributedCache
    {
        byte[] Set(string key, object state, Func<ICacheContext, byte[]> create);

        bool TryGetValue(string key, out byte[] value);

        void Refresh(string key);

        void Remove(string key);
    }
}