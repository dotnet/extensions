// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

#pragma warning disable LA0001 // Use the 'Microsoft.Shared.Diagnostics.Throws' class instead of explicitly throwing exception for improved performance

namespace Microsoft.Extensions.AI.Utilities;

/// <summary>
/// Defines a cache holding transformed JSON schemas corresponding to <see cref="AIFunction"/> instances based on the specified policy.
/// </summary>
public sealed class AIFunctionSchemaTransformerCache
{
    private readonly ConditionalWeakTable<AIFunction, object> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionSchemaTransformerCache"/> class with the specified options.
    /// </summary>
    /// <param name="transformOptions">The options governing schema transformation.</param>
    public AIFunctionSchemaTransformerCache(AIJsonSchemaTransformOptions transformOptions)
    {
        _ = Throw.IfNull(transformOptions);

        if (transformOptions == AIJsonSchemaTransformOptions.Default)
        {
            throw new ArgumentException("The options instance does not specify any transformations.", nameof(transformOptions));
        }

        TransformOptions = transformOptions;
    }

    /// <summary>
    /// Gets the options governing schema transformation.
    /// </summary>
    public AIJsonSchemaTransformOptions TransformOptions { get; }

    /// <summary>
    /// Gets or creates a transformed JSON schema for the specified <see cref="AIFunction"/> instance.
    /// </summary>
    /// <param name="function">The function whose JSON schema we want to transform.</param>
    /// <returns>The transformed JSON schema corresponding to <see cref="TransformOptions"/>.</returns>
    public JsonElement GetTransformedSchema(AIFunction function)
    {
        _ = Throw.IfNull(function);

        if (_cache.TryGetValue(function, out object? transformedSchemaObject))
        {
            return (JsonElement)transformedSchemaObject;
        }

        JsonElement transformedSchema = AIJsonUtilities.TransformSchema(function.JsonSchema, TransformOptions);
        _cache.Add(function, transformedSchema);
        return transformedSchema;
    }
}
