// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class AzureTableStorageConfigurationSource : IConfigurationSource
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string PartitionKey { get; set; }
        public CancellationToken CancellationToken { get; internal set; }

        public ITableStore<ConfigurationEntry> ConfigurationStore {  get; set;}

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new AzureTableStorageConfigurationProvider(new ConfigurationTableStore(ConnectionString), TableName, PartitionKey, CancellationToken);
    }
}
