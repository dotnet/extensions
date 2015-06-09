// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Framework.Caching.SqlServer
{
    public class SqlServerCacheTest
    {
        [Fact]
        public void AddSqlServerCache_AddsAsSingleRegistrationService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SqlServerCacheExtensions.AddSqlServerCacheServices(services);
            SqlServerCacheExtensions.AddSqlServerCacheServices(services);

            // Assert
            Assert.Equal(1, services.Count);
            var serviceDescriptor = services[0];
            Assert.Equal(typeof(IDistributedCache), serviceDescriptor.ServiceType);
            Assert.Equal(typeof(SqlServerCache), serviceDescriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }
    }
}
