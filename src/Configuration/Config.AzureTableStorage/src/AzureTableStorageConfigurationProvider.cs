using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class AzureTableStorageConfigurationProvider : ConfigurationProvider
    {
        private readonly ITableStore<ConfigurationEntry> configurationEntityStore;
        private readonly string tableName;
        private readonly string partitionKey;

        public AzureTableStorageConfigurationProvider(ITableStore<ConfigurationEntry> configurationEntityStore, string tableName, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException($"{nameof(tableName)} cannot be null or white space.", nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"{nameof(partitionKey)} cannot be null or white space.", nameof(partitionKey));
            }

            this.configurationEntityStore = configurationEntityStore ?? throw new ArgumentNullException(nameof(configurationEntityStore));
            this.tableName = tableName;
            this.partitionKey = partitionKey;
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task LoadAsync()
        {
            var allItems = await configurationEntityStore.GetAllByPartitionKey(tableName, partitionKey);
            Data = allItems.Where(x => x.IsActive).ToDictionary(x => x.RowKey, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
