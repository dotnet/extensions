// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace MemoryCacheSample
{
    public class Program
    {
        public static void Main()
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            object result;
            string key = "Key";
            object newObject = new object();
            object state = new object();

            // Basic CRUD operations:

            // Create / Overwrite
            result = cache.Set(key, newObject);
            result = cache.Set(key, new object());

            // Retrieve, null if not found
            result = cache.Get(key);

            // Retrieve
            bool found = cache.TryGetValue(key, out result);

            // Delete
            cache.Remove(key);

            // Cache entry configuration:

            // Stays in the cache as long as possible
            result = cache.Set(
                key,
                new object(),
                new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));

            // Automatically remove if not accessed in the given time
            result = cache.Set(
                key,
                new object(),
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));

            // Automatically remove at a certain time
            result = cache.Set(
                key,
                new object(),
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddDays(2)));

            // Automatically remove at a certain time, which is relative to UTC now
            result = cache.Set(
                key,
                new object(),
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromMinutes(10)));

            // Automatically remove if not accessed in the given time
            // Automatically remove at a certain time (if it lives that long)
            result = cache.Set(
                key,
                new object(),
                new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddDays(2)));

            // Callback when evicted
            var options = new MemoryCacheEntryOptions()
                .RegisterPostEvictionCallback(
                (echoKey, value, reason, substate) =>
                {
                    Console.WriteLine(echoKey + ": '" + value + "' was evicted due to " + reason);
                });
            result = cache.Set(key, new object(), options);

            // Remove on token expiration
            var cts = new CancellationTokenSource();
            options = new MemoryCacheEntryOptions()
                .AddExpirationToken(new CancellationChangeToken(cts.Token))
                .RegisterPostEvictionCallback(
                (echoKey, value, reason, substate) =>
                {
                    Console.WriteLine(echoKey + ": '" + value + "' was evicted due to " + reason);
                });
            result = cache.Set(key, new object(), options);

            // Fire the token to see the registered callback being invoked
            cts.Cancel();

            // Expire an entry if the dependent entry expires
            using (var link = cache.CreateLinkingScope())
            {
                cts = new CancellationTokenSource();
                cache.Set("key1", "value1", new MemoryCacheEntryOptions()
                    .AddExpirationToken(new CancellationChangeToken(cts.Token)));

                // expire this entry if the entry with key "key1" expires.
                cache.Set("key2", "value2", new MemoryCacheEntryOptions()
                    .AddEntryLink(link)
                    .RegisterPostEvictionCallback(
                    (echoKey, value, reason, substate) =>
                    {
                        Console.WriteLine(echoKey + ": '" + value + "' was evicted due to " + reason);
                    }));
            }

            // Fire the token to see the registered callback being invoked
            cts.Cancel();
        }
    }
}
