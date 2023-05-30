// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public interface ITableLocator
{
    TableInfo? LocateTable(in TableInfo options, RequestOptions request);
}
