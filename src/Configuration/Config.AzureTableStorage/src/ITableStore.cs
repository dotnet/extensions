using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal interface ITableStore<T> where T : TableEntity
    {        
        Task<IEnumerable<T>> GetAllByPartitionKey(string tableName, string partitionKey);
    }
}
