// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration.AzureTableStorage;
using Moq;
using Xunit;

namespace AzureTableStorageConfiguration.Tests
{
    [Trait("Category", "Unit Tests")]
    public class AzureTableStorageConfigurationTest
    {
        private readonly Mock<ITableStore<ConfigurationEntry>> _tableStore;
        private readonly CancellationToken _noCancelToken;
        private readonly CancellationToken _cancelToken;

        public AzureTableStorageConfigurationTest()
        {
            _tableStore = new Mock<ITableStore<ConfigurationEntry>>();
            _noCancelToken = CancellationToken.None;
            _cancelToken = new CancellationToken(true);
        }

        [Fact]
        public void Should_Load_Correctly_Without_Cancellation_Token()
        {                     
            _tableStore.Setup(x => x.GetAllByPartitionKey("ConfigTableName", "ConfigPartitionKey", _noCancelToken)).ReturnsAsync(new List<ConfigurationEntry> 
            {
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config1", Value = "ConfigValue1", IsActive = true },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config2", Value = "ConfigValue2", IsActive = false },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config3", Value = "ConfigValue3", IsActive = true },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config4", Value = "ConfigValue4", IsActive = false }
            });

            var provider = new AzureTableStorageConfigurationProvider(_tableStore.Object, "ConfigTableName", "ConfigPartitionKey", _noCancelToken);

            provider.Load();

            _tableStore.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();

            // Loads active items
            Assert.Equal(new[] { "Config1", "Config3" }, childKeys);

            // Gets correct value for active items
            var activeItemResult = provider.TryGet("Config3", out var activeValue);

            Assert.True(activeItemResult);
            Assert.Equal("ConfigValue3", activeValue);

            // Returns null for inactive items
            var inactiveItemResult = provider.TryGet("Config2", out var inactiveValue);
            Assert.Null(inactiveValue);

            // Returns null for non-existent items
            var nonExistentItemResult = provider.TryGet("Config5", out var nonExistentValue);
            Assert.Null(nonExistentValue);
        }
    }
}
