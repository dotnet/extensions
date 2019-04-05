using Moq;
using Xunit;
using Microsoft.Extensions.Configuration.AzureTableStorage;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AzureTableStorageConfiguration.Tests
{
    [Trait("Category", "Unit Tests")]
    public class AzureTableStorageConfigurationTest
    {
        [Fact]
        public void LoadsAllConfigFromTableStorage()
        {         
            var tableStore = new Mock<ITableStore<ConfigurationEntry>>();
            tableStore.Setup(x => x.GetAllByPartitionKey("ConfigTableName", "ConfigPartitionKey")).ReturnsAsync(new List<ConfigurationEntry> 
            {
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config1", Value = "ConfigValue1", IsActive = true },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config2", Value = "ConfigValue2", IsActive = false },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config3", Value = "ConfigValue3", IsActive = true },
                new ConfigurationEntry { PartitionKey = "ConfigPartitionKey", RowKey = "Config4", Value = "ConfigValue4", IsActive = false }
            });

            var provider = new AzureTableStorageConfigurationProvider(tableStore.Object, "ConfigTableName", "ConfigPartitionKey");

            provider.Load();

            tableStore.VerifyAll();

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
