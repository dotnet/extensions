// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// Enum representing patch operation type.
/// </summary>
public enum PatchOperationType
{
    /// <summary>
    /// Represents add operation.
    /// </summary>
    Add = 0,
    /// <summary>
    /// Represents remove operation.
    /// </summary>
    Remove = 1,
    /// <summary>
    /// Represents replace operation.
    /// </summary>
    Replace = 2,
    /// <summary>
    /// Represents set operation.
    /// </summary>
    Set = 3,
    /// <summary>
    /// Represents increment operation.
    /// </summary>
    Increment = 4
}
