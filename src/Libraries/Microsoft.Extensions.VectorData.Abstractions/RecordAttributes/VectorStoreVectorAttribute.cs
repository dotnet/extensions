// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// Defines an attribute to mark a property on a record class as a vector.
/// </summary>
/// <remarks>
/// The characteristics defined here influence how the property is treated by the vector store.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class VectorStoreVectorAttribute : Attribute
{
#pragma warning disable IDE1006 // Naming Styles
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreVectorAttribute"/> class.
    /// </summary>
    /// <param name="Dimensions">The number of dimensions that the vector has.</param>
    public VectorStoreVectorAttribute(int Dimensions)
    {
        if (Dimensions <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(Dimensions), "Dimensions must be greater than zero.");
        }

        this.Dimensions = Dimensions;
    }
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    /// Gets the number of dimensions that the vector has.
    /// </summary>
    /// <remarks>
    /// This property is required when creating collections, but can be omitted if not using that functionality.
    /// If not provided when trying to create a collection, create will fail.
    /// </remarks>
    public int Dimensions { get; }

    /// <summary>
    /// Gets the kind of index to use.
    /// </summary>
    /// <value>
    /// The default value varies by database type. See the documentation of your chosen database provider for more information.
    /// </value>
    /// <seealso cref="IndexKind"/>
#pragma warning disable CA1019 // Define accessors for attribute arguments: The constructor overload that contains this property is obsolete.
    public string? IndexKind { get; init; }
#pragma warning restore CA1019

    /// <summary>
    /// Gets the distance function to use when comparing vectors.
    /// </summary>
    /// <value>
    /// The default value varies by database type. See the documentation of your chosen database provider for more information.
    /// </value>
    /// <seealso cref="DistanceFunction"/>
#pragma warning disable CA1019 // Define accessors for attribute arguments: The constructor overload that contains this property is obsolete.
    public string? DistanceFunction { get; init; }
#pragma warning restore CA1019

    /// <summary>
    /// Gets an optional name to use for the property in storage, if different from the property name.
    /// </summary>
    /// <remarks>
    /// For example, the property name might be "MyProperty" and the storage name might be "my_property".
    /// </remarks>
    public string? StorageName { get; init; }
}
