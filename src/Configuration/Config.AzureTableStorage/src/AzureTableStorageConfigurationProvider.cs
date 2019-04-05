using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class AzureTableStorageConfigurationProvider : ConfigurationProvider
    {
        private readonly ITableStore<ConfigurationEntry> _configurationEntityStore;
        private readonly string _tableName;
        private readonly string _partitionKey;
        private readonly CancellationToken _cancellationToken;

        public AzureTableStorageConfigurationProvider(ITableStore<ConfigurationEntry> configurationEntityStore, string tableName, string partitionKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException($"{nameof(tableName)} cannot be null or white space.", nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException($"{nameof(partitionKey)} cannot be null or white space.", nameof(partitionKey));
            }

            _configurationEntityStore = configurationEntityStore ?? throw new ArgumentNullException(nameof(configurationEntityStore));
            _tableName = tableName;
            _partitionKey = partitionKey;
            _cancellationToken = cancellationToken;
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        
        private async Task LoadAsync()
        {
            var allItems = await _configurationEntityStore.GetAllByPartitionKey(_tableName, _partitionKey, _cancellationToken).ConfigureAwait(false);
            Data = allItems.Where(x => x.IsActive).ToDictionary(x => x.RowKey, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
