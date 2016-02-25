// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Caching.SqlServer
{
    public class SqlServerCacheServicesExtensionsTest
    {
        [Fact]
        public void AddDistributedSqlServerCache_AddsAsSingleRegistrationService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SqlServerCachingServicesExtensions.AddSqlServerCacheServices(services);

            // Assert
            Assert.Equal(1, services.Count);
            var serviceDescriptor = services[0];
            Assert.Equal(typeof(IDistributedCache), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(SqlServerCache), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [Fact]
        public void AddDistributedSqlServerCache_ReplacesPreviouslyUserRegisteredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped(typeof(IDistributedCache), sp => Mock.Of<IDistributedCache>());

            // Act
            services.AddDistributedSqlServerCache(options => {
                options.ConnectionString = "Fake";
                options.SchemaName = "Fake";
                options.TableName = "Fake";
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Scoped, distributedCache.Lifetime);
            Assert.IsType<SqlServerCache>(serviceProvider.GetRequiredService<IDistributedCache>());
        }
    }
}
