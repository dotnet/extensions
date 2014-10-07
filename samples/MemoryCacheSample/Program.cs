using System;
using System.Threading;
using Microsoft.Framework.Cache.Memory;

namespace MemoryCacheSample
{
    public class Program
    {
        public void Main()
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            object result;
            string key = "Key";
            object newObject = new object();
            object state = new object();

            // Basic CRUD operations:

            // Create / Overwrite
            result = cache.Set(key, newObject);
            result = cache.Set(key, context => new object());
            result = cache.Set(key, state, context => new object());

            // Retrieve, null if not found
            result = cache.Get(key);

            // Retrieve
            bool found = cache.TryGetValue(key, out result);

            // Delete
            cache.Remove(key);

            // Conditional operations:

            // Retrieve / Create when we want to lazily create the object.
            result = cache.GetOrSet(key, context => new object());

            // Retrieve / Create when we want to lazily create the object.
            result = cache.GetOrSet(key, state, context => new object());

            // Cache entry configuration:

            // Stays in the cache as long as possible
            result = cache.GetOrSet(key, state, context =>
            {
                context.SetPriority(CachePreservationPriority.NeverRemove);
                return new object();
            });

            // Automatically remove if not accessed in the given time
            result = cache.GetOrSet(key, state, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromMinutes(5));
                return new object();
            });

            // Automatically remove at a certain time
            result = cache.GetOrSet(key, state, context =>
            {
                context.SetAbsoluteExpiration(new DateTime(2014, 12, 31));
                // or relative:
                // context.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                return new object();
            });

            // Automatically remove if not accessed in the given time
            // Automatically remove at a certain time (if it lives that long)
            result = cache.GetOrSet(key, state, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromMinutes(5));

                context.SetAbsoluteExpiration(new DateTime(2014, 12, 31));
                // or relative:
                // context.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                return new object();
            });

            // Callback when evicted
            result = cache.GetOrSet(key, state, context =>
            {
                context.RegisterPostEvictionCallback((echoKey, value, reason, substate) =>
                    Console.WriteLine(echoKey + ": '" + value + "' was evicted due to " + reason), state: null);
                return new object();
            });

            // Remove on trigger
            var cts = new CancellationTokenSource();
            result = cache.GetOrSet(key, state, context =>
            {
                context.AddExpirationTrigger(new CancellationTokenTrigger(cts.Token));
                return new object();
            });

            result = cache.GetOrSet(key, context =>
            {
                var link = new EntryLink();

                var inner1 = cache.GetOrSet("subkey1", link, subContext =>
                {
                    return "SubValue1";
                });

                string inner2;
                using (link.FlowContext())
                {
                    inner2 = cache.GetOrSet("subkey2", subContext =>
                    {
                        return "SubValue2";
                    });
                }

                context.AddEntryLink(link);

                return inner1 + inner2;
            });
        }
    }
}
