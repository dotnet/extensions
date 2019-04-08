// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal interface ITableStore<T> where T : TableEntity
    {        
        Task<IEnumerable<T>> GetAllByPartitionKey(string tableName, string partitionKey, CancellationToken cancellationToken);
    }
}
