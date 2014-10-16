using System;
using System.IO;
using Microsoft.Framework.Cache.Redis;

namespace RedisCacheSample
{
    public class Program
    {
        /// <summary>
        /// This sample assumes that a redis server is running on the local machine. You can set this up by doing the following:
        /// Install this chocolatey package: http://chocolatey.org/packages/redis-64/
        /// run "redis-server" from command prompt.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            string key = "myKey";
            object state = null;
            byte[] value = new byte[10];
            Stream valueStream;

            Console.WriteLine("Connecting to cache");
            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = "localhost",
                InstanceName = "SampleInstance"
            });
            Console.WriteLine("Connected");

            Console.WriteLine("Setting");
            valueStream = cache.Set(key, state, context =>
            {
                context.Data.Write(value, 0, value.Length);
            });
            Console.WriteLine("Set");

            Console.WriteLine("Getting");
            if (cache.TryGetValue(key, out valueStream))
            {
                Console.WriteLine("Retrieved: " + valueStream);
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.WriteLine("Refreshing");
            cache.Refresh(key);
            Console.WriteLine("Refreshed");

            Console.WriteLine("Removing");
            cache.Remove(key);
            Console.WriteLine("Removed");

            Console.WriteLine("Getting");
            if (cache.TryGetValue(key, out valueStream))
            {
                Console.WriteLine("Retrieved: " + valueStream);
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.ReadLine();
        }
    }
}
