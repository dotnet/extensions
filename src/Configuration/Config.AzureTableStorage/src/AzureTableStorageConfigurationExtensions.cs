using Microsoft.Extensions.Configuration.AzureTableStorage;
using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Configuration extensions for AzureTableStorage configuration provider
    /// </summary>
    public static class AzureTableStorageConfigurationExtensions
    {
        /// <summary>
        /// Adds AzureTableStorage configuration to the configuration builder
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <param name="connectionString">Connection string to the storage account with table storage enabled</param>
        /// <param name="tableName">Table name to retreive values from</param>
        /// <param name="partitionKey">Partition key to retreieve values from</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAzureTableStorage(this IConfigurationBuilder configurationBuilder, string connectionString, string tableName = null, string partitionKey = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} cannot be null or white space.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "AzureTableStorageConfiguration";
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                partitionKey = "AzureTableStorageConfigurationPartition";
            }

            return configurationBuilder.Add(new AzureTableStorageConfigurationSource
            {
                ConnectionString = connectionString,
                TableName = tableName,
                PartitionKey = partitionKey
            });
        }
    }
}
