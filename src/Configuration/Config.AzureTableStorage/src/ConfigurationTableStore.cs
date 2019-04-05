using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class ConfigurationTableStore : ITableStore<ConfigurationEntry>
    {
        private const int RetryBackOffSeconds = 1;
        private const int RetryMaxAttempts = 3;
        private readonly CloudTableClient cloudTableClient;

        public ConfigurationTableStore(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} cannot be null or white space.", nameof(connectionString));
            }

            // Parse storage account
            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            var requestOptions = new TableRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(RetryBackOffSeconds), RetryMaxAttempts)
            };


            // Create table client
            cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            cloudTableClient.DefaultRequestOptions = requestOptions;
        }

        public async Task<IEnumerable<ConfigurationEntry>> GetAllByPartitionKey(string tableName, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException($"{nameof(tableName)} cannot be null or white space.", nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"{nameof(partitionKey)} cannot be null or white space.", nameof(partitionKey));
            }

            if (cloudTableClient == null)
            {
                throw new Exception($"{nameof(cloudTableClient)} hasn't been initialized correctly.");
            }

            var cloudTable = cloudTableClient.GetTableReference(tableName);

            await cloudTable.CreateIfNotExistsAsync();

            var query = new TableQuery<ConfigurationEntry>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            var allItems = new List<ConfigurationEntry>();

            TableContinuationToken continuationToken = null;

            do
            {
                var items = await cloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = items.ContinuationToken;
                allItems.AddRange(items);
            } while (continuationToken != null);

            return allItems;
        }
    }
}
