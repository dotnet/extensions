// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.Cache.Distributed
{
    [AssemblyNeutral]
    public interface ICacheContext
    {
        /// <summary>
        /// The key identifying this entry.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The state passed into Set. This can be used to avoid closures.
        /// </summary>
        object State { get; }

        /// <summary>
        /// Gets a stream to write the cache entry data to.
        /// </summary>
        Stream Data { get; }

        /// <summary>
        /// Sets an absolute expiration date for this entry.
        /// </summary>
        /// <param name="absolute"></param>
        void SetAbsoluteExpiration(DateTimeOffset absolute);

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="relative"></param>
        void SetAbsoluteExpiration(TimeSpan relative);

        /// <summary>
        /// Sets how long this entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="offset"></param>
        void SetSlidingExpiration(TimeSpan offset);
    }
}