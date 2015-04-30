// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Framework.Caching.Distributed
{
    public class CacheServiceExtensionsTests
    {
        [Fact]
        public void AddCaching_RegistersMemoryCacheAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCaching();

            // Assert
            var memoryCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IMemoryCache));

            Assert.NotNull(memoryCache);
            Assert.Equal(ServiceLifetime.Singleton, memoryCache.Lifetime);
        }

        [Fact]
        public void AddCaching_RegistersDistributedCacheAsTransient()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCaching();

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
            services.AddCaching();

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

        private class TestMemoryCache : IMemoryCache
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void Remove(string key)
            {
                throw new NotImplementedException();
            }

            public object Set(string key, IEntryLink link, object state, Func<ICacheSetContext, object> create)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, IEntryLink link, out object value)
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

            public void Refresh(string key)
            {
                throw new NotImplementedException();
            }

            public void Remove(string key)
            {
                throw new NotImplementedException();
            }

            public Stream Set(string key, object state, Action<ICacheContext> create)
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
