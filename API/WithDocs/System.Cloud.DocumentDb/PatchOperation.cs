// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Describes patch operation details.
/// </summary>
public readonly struct PatchOperation
{
    /// <summary>
    /// Gets the patch operation type.
    /// </summary>
    public PatchOperationType OperationType { get; }

    /// <summary>
    /// Gets the patch operation path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the patch operation value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Creates patch details for add operation.
    /// </summary>
    /// <typeparam name="T">The type of value to be patched.</typeparam>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The value to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Add<T>(string path, T value) where T : notnull;

    /// <summary>
    /// Creates patch details for remove operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Remove(string path);

    /// <summary>
    /// Creates patch details for replace operation.
    /// </summary>
    /// <typeparam name="T">The type of value to be patched.</typeparam>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The value to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Replace<T>(string path, T value) where T : notnull;

    /// <summary>
    /// Creates patch details for set operation.
    /// </summary>
    /// <typeparam name="T">The type of value to be patched.</typeparam>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The value to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Set<T>(string path, T value) where T : notnull;

    /// <summary>
    /// Creates patch details for increment by long operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The long value to be incremented by.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Increment(string path, long value);

    /// <summary>
    /// Creates patch details for increment by double operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The double value to be incremented by.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Increment(string path, double value);
}
