// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// Define possible consistency levels.
/// </summary>
/// <remarks>
/// Supported values may vary for different APIs and Engines.
/// If requested level is not supported by an API, the API should throw
/// <see cref="T:System.Cloud.DocumentDb.DatabaseClientException" /> indicating supported options.
/// </remarks>
public enum ConsistencyLevel
{
    /// <summary>
    /// Defines a Strong Consistency for operation.
    /// </summary>
    /// <remarks>
    /// Strong Consistency guarantees that read operations always return the value that was last written.
    /// </remarks>
    Strong = 0,
    /// <summary>
    /// Defines a Bounded Staleness Consistency for operation.
    /// </summary>
    /// <remarks>
    /// Bounded Staleness guarantees that reads are not too out-of-date.
    /// </remarks>
    BoundedStaleness = 1,
    /// <summary>
    /// Defines a Session Consistency for operation.
    /// </summary>
    /// <remarks>
    /// Session Consistency guarantees monotonic reads, all reads and writes
    /// in a scope of session executed in the order they came.
    /// If a session is specified, reads never gets an old data.
    /// </remarks>
    Session = 2,
    /// <summary>
    /// Defines a Eventual Consistency for operation.
    /// </summary>
    /// <remarks>
    /// Eventual Consistency guarantees if no new updates are made to a given data item,
    /// eventually all accesses to that item will return the last updated value.
    /// </remarks>
    Eventual = 3,
    /// <summary>
    /// Defines a Consistent Prefix Consistency for operation.
    /// </summary>
    /// <remarks>
    /// Consistent Prefix Consistency guarantees that reads will return some prefix of
    /// all writes with no gaps. All writes will be eventually be available for reads.
    /// </remarks>
    ConsistentPrefix = 4
}
