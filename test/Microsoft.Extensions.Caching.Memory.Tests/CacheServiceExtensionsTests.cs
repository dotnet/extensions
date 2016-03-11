// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Caching.Distributed
{
    public class CacheServiceExtensionsTests
    {
        [Fact]
        public void AddMemoryCache_RegistersMemoryCacheAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMemoryCache();

            // Assert
            var memoryCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IMemoryCache));

            Assert.NotNull(memoryCache);
            Assert.Equal(ServiceLifetime.Singleton, memoryCache.Lifetime);
        }

        [Fact]
        public void AddDistributedMemoryCache_RegistersDistributedCacheAsTransient()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDistributedMemoryCache();

            // Assert
            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Transient, distributedCache.Lifetime);
        }

        [Fact]
        public void AddCaching_DoesNotReplaceUserRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IMemoryCache, TestMemoryCache>();
            services.AddScoped<IDistributedCache, TestDistributedCache>();

            // Act
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var memoryCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IMemoryCache));
            Assert.NotNull(memoryCache);
            Assert.Equal(ServiceLifetime.Scoped, memoryCache.Lifetime);
            Assert.IsType<TestMemoryCache>(serviceProvider.GetRequiredService<IMemoryCache>());

            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));
            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Scoped, memoryCache.Lifetime);
            Assert.IsType<TestDistributedCache>(serviceProvider.GetRequiredService<IDistributedCache>());
        }

        [Fact]
        public void AddMemoryCache_allows_chaining()
        {
            var services = new ServiceCollection();

            Assert.Same(services, services.AddMemoryCache());
        }

        [Fact]
        public void AddMemoryCache_with_action_allows_chaining()
        {
            var services = new ServiceCollection();

            Assert.Same(services, services.AddMemoryCache(_ => { }));
        }

        [Fact]
        public void AddDistributedMemoryCache_allows_chaining()
        {
            var services = new ServiceCollection();

            Assert.Same(services, services.AddDistributedMemoryCache());
        }

        private class TestMemoryCache : IMemoryCache
        {
            public IEntryLink CreateLinkingScope()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void Remove(object key)
            {
                throw new NotImplementedException();
            }

            public object Set(object key, object value, MemoryCacheEntryOptions cacheEntryOptions)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(object key, out object value)
            {
                throw new NotImplementedException();
            }
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

            public bool TryGetValue(string key, out byte[] value)
            {
                throw new NotImplementedException();
            }

            public Task<bool> TryGetValueAsync(string key, out byte[] value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
