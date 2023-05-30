// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

public enum ConsistencyLevel
{
    Strong = 0,
    BoundedStaleness = 1,
    Session = 2,
    Eventual = 3,
    ConsistentPrefix = 4
}
