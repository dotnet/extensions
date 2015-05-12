// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.DependencyInjection;
using Xunit;
namespace Microsoft.Framework.Caching.Redis
{
    public class CacheServiceExtensionsTests
    {
        

        [Fact]
        public void AddRedisCache_RegistersDistributedCacheAsTransient()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRedisCache();

            // Assert
            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Transient, distributedCache.Lifetime);
        }

        [Fact]
        public void AddRedisCache_DoesNotReplaceUserRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IDistributedCache, TestDistributedCache>();

            // Act
            services.AddCaching();

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
