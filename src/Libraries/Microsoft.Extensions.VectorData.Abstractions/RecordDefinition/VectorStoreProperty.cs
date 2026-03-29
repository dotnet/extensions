// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// Defines a base property class for properties on a vector store record.
/// </summary>
/// <remarks>
/// The characteristics defined here influence how the property is treated by the vector store.
/// </remarks>
public abstract class VectorStoreProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreProperty"/> class.
    /// </summary>
    /// <param name="name">The name of the property on the data model. If the record is mapped to a .NET type, this corresponds to the .NET property name on that type.</param>
    /// <param name="type">The type of the property.</param>
    private protected VectorStoreProperty(string name, Type? type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Throw.ArgumentException(nameof(name), "Value cannot be null or whitespace.");
        }

        Name = name;
        Type = type;
    }

    private protected VectorStoreProperty(VectorStoreProperty source)
    {
        Name = source.Name;
        StorageName = source.StorageName;
        Type = source.Type;
        ProviderAnnotations = source.ProviderAnnotations is not null
            ? new Dictionary<string, object?>(source.ProviderAnnotations)
            : null;
    }

    /// <summary>
    /// Gets or sets the name of the property on the data model.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional name to use for the property in storage, if different from the property name.
    /// </summary>
    /// <remarks>
    /// For example, the property name might be "MyProperty" and the storage name might be "my_property".
    /// This property is only respected by implementations that don't support a well-known
    /// serialization mechanism like JSON, in which case the attributes used by that serialization system will
    /// be used.
    /// </remarks>
    public string? StorageName { get; set; }

    /// <summary>
    /// Gets or sets the type of the property.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of provider-specific annotations for this property.
    /// </summary>
    /// <remarks>
    /// This allows setting database-specific configuration options that aren't universal across all vector stores.
    /// Use provider-specific extension methods to set and get values in a strongly-typed manner.
    /// </remarks>
    public Dictionary<string, object?>? ProviderAnnotations { get; set; }
}
