// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines the context in which a JSON schema within a type graph is being generated.
/// </summary>
public readonly struct AIJsonSchemaCreateContext
{
    private readonly JsonSchemaExporterContext _exporterContext;

    internal AIJsonSchemaCreateContext(JsonSchemaExporterContext exporterContext)
    {
        _exporterContext = exporterContext;
    }

    /// <summary>
    /// Gets the path to the schema document currently being generated.
    /// </summary>
    public ReadOnlySpan<string> Path => _exporterContext.Path;

    /// <summary>
    /// Gets the <see cref="JsonTypeInfo"/> for the type being processed.
    /// </summary>
    public JsonTypeInfo TypeInfo => _exporterContext.TypeInfo;

    /// <summary>
    /// Gets the type info for the polymorphic base type if generated as a derived type.
    /// </summary>
    public JsonTypeInfo? BaseTypeInfo => _exporterContext.BaseTypeInfo;

    /// <summary>
    /// Gets the <see cref="JsonPropertyInfo"/> if the schema is being generated for a property.
    /// </summary>
    public JsonPropertyInfo? PropertyInfo => _exporterContext.PropertyInfo;

    /// <summary>
    /// Gets the declaring type of the property or parameter being processed.
    /// </summary>
    public Type? DeclaringType =>
#if NET9_0_OR_GREATER
        _exporterContext.PropertyInfo?.DeclaringType;
#else
        _exporterContext.DeclaringType;
#endif

    /// <summary>
    /// Gets the <see cref="ICustomAttributeProvider"/> corresponding to the property or field being processed.
    /// </summary>
    public ICustomAttributeProvider? PropertyAttributeProvider =>
#if NET9_0_OR_GREATER
        _exporterContext.PropertyInfo?.AttributeProvider;
#else
        _exporterContext.PropertyAttributeProvider;
#endif

    /// <summary>
    /// Gets the <see cref="System.Reflection.ICustomAttributeProvider"/> of the
    /// constructor parameter associated with the accompanying <see cref="PropertyInfo"/>.
    /// </summary>
    public ICustomAttributeProvider? ParameterAttributeProvider =>
#if NET9_0_OR_GREATER
        _exporterContext.PropertyInfo?.AssociatedParameter?.AttributeProvider;
#else
        _exporterContext.ParameterInfo;
#endif

    /// <summary>
    /// Retrieves a custom attribute of a specified type that is applied to the specified schema node context.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
    /// <param name="inherit">If <see langword="true"/>, specifies to also search the ancestors of the context members for custom attributes.</param>
    /// <returns>The first occurrence of <typeparamref name="TAttribute"/> if found, or <see langword="null"/> otherwise.</returns>
    /// <remarks>
    /// This helper method resolves attributes from context locations in the following order:
    /// <list type="number">
    /// <item>Attributes specified on the property of the context, if specified.</item>
    /// <item>Attributes specified on the constructor parameter of the context, if specified.</item>
    /// <item>Attributes specified on the type of the context.</item>
    /// </list>
    /// </remarks>
    public TAttribute? GetCustomAttribute<TAttribute>(bool inherit = false)
        where TAttribute : Attribute
    {
        return GetCustomAttr(PropertyAttributeProvider) ??
            GetCustomAttr(ParameterAttributeProvider) ??
            GetCustomAttr(TypeInfo.Type);

        TAttribute? GetCustomAttr(ICustomAttributeProvider? provider) =>
            (TAttribute?)provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault();
    }
}
