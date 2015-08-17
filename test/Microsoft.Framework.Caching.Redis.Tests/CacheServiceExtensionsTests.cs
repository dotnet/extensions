// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Framework.Caching.Redis
{
    public class CacheServiceExtensionsTests
    {
        [Fact]
        public void AddRedisCache_RegistersDistributedCacheAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRedisCache();

            // Assert
            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
        }

        [Fact]
        public void AddRedisCache_DoesNotReplaceUserRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IDistributedCache, TestDistributedCache>();

            // Act
            services.AddRedisCache();

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Scoped, distributedCache.Lifetime);
            Assert.IsType<TestDistributedCache>(serviceProvider.GetRequiredService<IDistributedCache>());
        }

        private class TestDistributedCache : IDistributedCache
        {
            public void Connect()
            {
                throw new NotImplementedException();
            }

            public Task ConnectAsync()
            {
                throw new NotImplementedException();
            }

            public byte[] Get(string key)
            {
                throw new NotImplementedException();
            }

            public Task<byte[]> GetAsync(string key)
            {
                throw new NotImplementedException();
            }

            public void Refresh(string key)
            {
                throw new NotImplementedException();
            }

            public Task RefreshAsync(string key)
            {
                throw new NotImplementedException();
            }

            public void Remove(string key)
            {
                throw new NotImplementedException();
            }

            public Task RemoveAsync(string key)
            {
                throw new NotImplementedException();
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                throw new NotImplementedException();
            }

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out Stream value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
