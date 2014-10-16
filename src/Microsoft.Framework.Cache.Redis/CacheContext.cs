// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Cache.Distributed;
using StackExchange.Redis;

namespace Microsoft.Framework.Cache.Redis
{
    internal class CacheContext : ICacheContext
    {
        private readonly MemoryStream _data = new MemoryStream();

        internal CacheContext(string key)
        {
            Key = key;
            CreationTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// The key identifying this entry.
        /// </summary>
        public string Key { get; internal set; }

        /// <summary>
        /// The state passed into Set. This can be used to avoid closures.
        /// </summary>
        public object State { get; internal set; }

        public Stream Data { get { return _data; } }

        internal DateTimeOffset CreationTime { get; set; }

        internal DateTimeOffset? AbsoluteExpiration { get; private set; }

        internal TimeSpan? SlidingExpiration { get; private set; }

        public void SetAbsoluteExpiration(TimeSpan relative)
        {
            if (relative <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("relative", relative, "The relative expiration value must be positive.");
            }
            AbsoluteExpiration = CreationTime + relative;
        }

        public void SetAbsoluteExpiration(DateTimeOffset absolute)
        {
            if (absolute <= CreationTime)
            {
                throw new ArgumentOutOfRangeException("absolute", absolute, "The absolute expiration value must be in the future.");
            }
            AbsoluteExpiration = absolute.ToUniversalTime();
        }

        public void SetSlidingExpiration(TimeSpan offset)
        {
            if (offset <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "The sliding expiration value must be positive.");
            }
            SlidingExpiration = offset;
        }

        internal long? GetExpirationInSeconds()
        {
            if (AbsoluteExpiration.HasValue && SlidingExpiration.HasValue)
            {
                return (long)Math.Min((AbsoluteExpiration.Value - CreationTime).TotalSeconds, SlidingExpiration.Value.TotalSeconds);
            }
            else if (AbsoluteExpiration.HasValue)
            {
                return (long)(AbsoluteExpiration.Value - CreationTime).TotalSeconds;
            }
            else if (SlidingExpiration.HasValue)
            {
                return (long)SlidingExpiration.Value.TotalSeconds;
            }
            return null;
        }

        internal byte[] GetBytes()
        {
            return _data.ToArray();
        }
    }
}