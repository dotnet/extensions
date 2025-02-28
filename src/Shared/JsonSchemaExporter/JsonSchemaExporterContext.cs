// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET9_0_OR_GREATER
using System;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Schema;

/// <summary>
/// Defines the context in which a JSON schema within a type graph is being generated.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal readonly struct JsonSchemaExporterContext
{
#pragma warning disable IDE1006 // Naming Styles
    internal readonly string[] _path;
#pragma warning restore IDE1006 // Naming Styles

    internal JsonSchemaExporterContext(
        JsonTypeInfo typeInfo,
        JsonTypeInfo? baseTypeInfo,
        Type? declaringType,
        JsonPropertyInfo? propertyInfo,
        ParameterInfo? parameterInfo,
        ICustomAttributeProvider? propertyAttributeProvider,
        string[] path)
    {
        TypeInfo = typeInfo;
        DeclaringType = declaringType;
        BaseTypeInfo = baseTypeInfo;
        PropertyInfo = propertyInfo;
        ParameterInfo = parameterInfo;
        PropertyAttributeProvider = propertyAttributeProvider;
        _path = path;
    }

    /// <summary>
    /// Gets the path to the schema document currently being generated.
    /// </summary>
    public ReadOnlySpan<string> Path => _path;

    /// <summary>
    /// Gets the <see cref="JsonTypeInfo"/> for the type being processed.
    /// </summary>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the declaring type of the property or parameter being processed.
    /// </summary>
    public Type? DeclaringType { get; }

    /// <summary>
    /// Gets the type info for the polymorphic base type if generated as a derived type.
    /// </summary>
    public JsonTypeInfo? BaseTypeInfo { get; }

    /// <summary>
    /// Gets the <see cref="JsonPropertyInfo"/> if the schema is being generated for a property.
    /// </summary>
    public JsonPropertyInfo? PropertyInfo { get; }

    /// <summary>
    /// Gets the <see cref="System.Reflection.ParameterInfo"/> if a constructor parameter
    /// has been associated with the accompanying <see cref="PropertyInfo"/>.
    /// </summary>
    public ParameterInfo? ParameterInfo { get; }

    /// <summary>
    /// Gets the <see cref="ICustomAttributeProvider"/> corresponding to the property or field being processed.
    /// </summary>
    public ICustomAttributeProvider? PropertyAttributeProvider { get; }
}
#endif
