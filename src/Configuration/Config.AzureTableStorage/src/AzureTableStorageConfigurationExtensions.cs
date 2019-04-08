// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Configuration.AzureTableStorage;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Configuration extensions for AzureTableStorage configuration provider
    /// </summary>
    public static class AzureTableStorageConfigurationExtensions
    {
        private const string DefaultTableName = "AzureTableStorageConfiguration";
        private const string DefaultPartitionKey = "AzureTableStorageConfigurationPartition";

        /// <summary>
        ///   Adds a <see cref="AzureTableStorageConfigurationProvider"/> <see cref="IConfigurationProvider"/> 
        ///   that reads configuration values from the Azure Table Storage.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="connectionString">Connection string to the storage account with table storage enabled.</param>
        /// <param name="tableName">Table name to retreive values from</param>
        /// <param name="partitionKey">Partition key to retreieve values from</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAzureTableStorage(this IConfigurationBuilder configurationBuilder, string connectionString, string tableName = null, string partitionKey = null)
            => AddAzureTableStorageInternal(configurationBuilder, connectionString, CancellationToken.None, tableName, partitionKey);

        /// <summary>
        ///   Adds a <see cref="AzureTableStorageConfigurationProvider"/> <see cref="IConfigurationProvider"/> 
        ///   that reads configuration values from the Azure Table Storage.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="connectionString">Connection string to the storage account with table storage enabled.</param>
        /// <param name="tableName">Table name to retreive values from</param>
        /// <param name="partitionKey">Partition key to retreieve values from</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAzureTableStorage(this IConfigurationBuilder configurationBuilder, string connectionString, CancellationToken cancellationToken, string tableName = null, string partitionKey = null)
            => AddAzureTableStorageInternal(configurationBuilder, connectionString, cancellationToken, tableName, partitionKey);

        private static IConfigurationBuilder AddAzureTableStorageInternal(this IConfigurationBuilder configurationBuilder, string connectionString, CancellationToken cancellationToken, string tableName = null, string partitionKey = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} cannot be null or white space.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = DefaultTableName;
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                partitionKey = DefaultPartitionKey;
            }

            return configurationBuilder.Add(new AzureTableStorageConfigurationSource
            {
                ConnectionString = connectionString,
                TableName = tableName,
                PartitionKey = partitionKey,
                CancellationToken = cancellationToken
            });
        }
    }
}
