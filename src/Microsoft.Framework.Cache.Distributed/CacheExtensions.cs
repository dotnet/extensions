// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Framework.Cache.Distributed
{
    public static class CacheExtensions
    {
        public static byte[] ReadAllBytes([NotNull] this Stream stream)
        {
            var memStream = stream as MemoryStream;
            if (memStream == null)
            {
                memStream = new MemoryStream();
                stream.CopyTo(memStream);
            }
            return memStream.ToArray();
        }

        public static byte[] ReadBytes([NotNull] this Stream stream, int count)
        {
            var output = new byte[count];
            int total = 0;
            while (total < count)
            {
                var read = stream.Read(output, total, count - total);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                total += read;
            }
            return output;
        }

        public static Stream Set(this IDistributedCache cache, string key, byte[] value)
        {
            return cache.Set(key, state: value, create: context =>
            {
                var data = (byte[])context.State;
                context.Data.Write(data, 0, data.Length);
            });
        }

        public static Stream Set(this IDistributedCache cache, string key, Action<ICacheContext> create)
        {
            return cache.Set(key, state: null, create: create);
        }

        public static Stream Get(this IDistributedCache cache, string key)
        {
            Stream value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static Stream GetOrSet(this IDistributedCache cache, string key, byte[] value)
        {
            Stream value1;
            if (cache.TryGetValue(key, out value1))
            {
                return value1;
            }
            cache.Set(key, value);
            return new MemoryStream(value, writable: false);
        }

        public static Stream GetOrSet(this IDistributedCache cache, string key, Action<ICacheContext> create)
        {
            Stream value;
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }
            return cache.Set(key, create);
        }

        public static Stream GetOrSet(this IDistributedCache cache, string key, object state, Action<ICacheContext> create)
        {
            Stream value;
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }
            return cache.Set(key, state, create);
        }
    }
}