// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Caching.SqlServer
{
    public class SqlServerCacheServicesExtensionsTest
    {
        [Fact]
        public void AddSqlServerCache_AddsAsSingleRegistrationService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SqlServerCachingServicesExtensions.AddSqlServerCacheServices(services);
            SqlServerCachingServicesExtensions.AddSqlServerCacheServices(services);

            // Assert
            Assert.Equal(1, services.Count);
            var serviceDescriptor = services[0];
            Assert.Equal(typeof(IDistributedCache), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(SqlServerCache), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }
    }
}
