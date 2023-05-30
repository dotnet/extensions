// Assembly 'System.Cloud.DocumentDb.Abstractions'

namespace System.Cloud.DocumentDb;

/// <summary>
/// The result interface including item for document storage responses.
/// </summary>
/// <typeparam name="T">The type of the item the response contains.</typeparam>
public interface IDatabaseResponse<out T> : IDatabaseResponse where T : notnull
{
    /// <summary>
    /// Gets response item.
    /// </summary>
    T? Item { get; }
}
