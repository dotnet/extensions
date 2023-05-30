// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// The operation used in <see cref="T:System.Cloud.DocumentDb.BatchItem`1" /> to indicate the action to perform.
/// </summary>
public enum BatchOperation
{
    /// <summary>
    /// Create item.
    /// </summary>
    Create = 0,
    /// <summary>
    /// Read item.
    /// </summary>
    Read = 1,
    /// <summary>
    /// Replace item.
    /// </summary>
    Replace = 2,
    /// <summary>
    /// Delete item.
    /// </summary>
    Delete = 3,
    /// <summary>
    /// Upsert item.
    /// </summary>
    Upsert = 4
}
