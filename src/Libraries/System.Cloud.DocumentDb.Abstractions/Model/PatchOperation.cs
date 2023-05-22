// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// Describes patch operation details.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "Not to be used as a key in key value collections.")]
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
    public static PatchOperation Add<T>(string path, T value)
        where T : notnull
    {
        return new PatchOperation(PatchOperationType.Add, path, value);
    }

    /// <summary>
    /// Creates patch details for remove operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Remove(string path)
    {
        return new PatchOperation(PatchOperationType.Remove, path, string.Empty);
    }

    /// <summary>
    /// Creates patch details for replace operation.
    /// </summary>
    /// <typeparam name="T">The type of value to be patched.</typeparam>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The value to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Replace<T>(string path, T value)
        where T : notnull
    {
        return new PatchOperation(PatchOperationType.Replace, path, value);
    }

    /// <summary>
    /// Creates patch details for set operation.
    /// </summary>
    /// <typeparam name="T">The type of value to be patched.</typeparam>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The value to be patched.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Set<T>(string path, T value)
        where T : notnull
    {
        return new PatchOperation(PatchOperationType.Set, path, value);
    }

    /// <summary>
    /// Creates patch details for increment by long operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The long value to be incremented by.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Increment(string path, long value)
    {
        return new PatchOperation(PatchOperationType.Increment, path, value);
    }

    /// <summary>
    /// Creates patch details for increment by double operation.
    /// </summary>
    /// <param name="path">The path to be patched.</param>
    /// <param name="value">The double value to be incremented by.</param>
    /// <returns>Created patch operation.</returns>
    public static PatchOperation Increment(string path, double value)
    {
        return new PatchOperation(PatchOperationType.Increment, path, value);
    }

    internal PatchOperation(PatchOperationType type, string path, object value)
    {
        OperationType = type;
        Path = path;
        Value = value;
    }
}
