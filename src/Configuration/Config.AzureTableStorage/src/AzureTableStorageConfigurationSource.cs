namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class AzureTableStorageConfigurationSource : IConfigurationSource
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string PartitionKey { get; set; }
        public ITableStore<ConfigurationEntry> ConfigurationStore {  get; set;}

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureTableStorageConfigurationProvider(new ConfigurationTableStore(ConnectionString), TableName, PartitionKey);
        }
    }
}
