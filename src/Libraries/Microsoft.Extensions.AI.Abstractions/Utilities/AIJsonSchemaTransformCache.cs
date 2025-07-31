// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines a cache for JSON schemas transformed according to the specified <see cref="AIJsonSchemaTransformOptions"/> policy.
/// </summary>
/// <remarks>
/// <para>
/// This cache stores weak references from AI abstractions that declare JSON schemas such as <see cref="AIFunction"/> or <see cref="ChatResponseFormatJson"/>
/// to their corresponding JSON schemas transformed according to the specified <see cref="TransformOptions"/> policy. It is intended for use by <see cref="IChatClient"/>
/// implementations that enforce vendor-specific restrictions on what constitutes a valid JSON schema for a given function or response format.
/// </para>
/// <para>
/// It is recommended <see cref="IChatClient"/> implementations with schema transformation requirements should create a single static instance of this cache.
/// </para>
/// </remarks>
public sealed class AIJsonSchemaTransformCache
{
    private readonly ConditionalWeakTable<AIFunction, object> _functionSchemaCache = new();
    private readonly ConditionalWeakTable<ChatResponseFormatJson, object> _responseFormatCache = new();

    private readonly ConditionalWeakTable<AIFunction, object>.CreateValueCallback _functionSchemaCreateValueCallback;
    private readonly ConditionalWeakTable<ChatResponseFormatJson, object>.CreateValueCallback _responseFormatCreateValueCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIJsonSchemaTransformCache"/> class with the specified options.
    /// </summary>
    /// <param name="transformOptions">The options governing schema transformation.</param>
    public AIJsonSchemaTransformCache(AIJsonSchemaTransformOptions transformOptions)
    {
        _ = Throw.IfNull(transformOptions);

        if (transformOptions == AIJsonSchemaTransformOptions.Default)
        {
            Throw.ArgumentException(nameof(transformOptions), "The options instance does not specify any transformations.");
        }

        TransformOptions = transformOptions;
        _functionSchemaCreateValueCallback = function => AIJsonUtilities.TransformSchema(function.JsonSchema, TransformOptions);
        _responseFormatCreateValueCallback = responseFormat => AIJsonUtilities.TransformSchema(responseFormat.Schema!.Value, TransformOptions);
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
    public JsonElement GetOrCreateTransformedSchema(AIFunction function)
    {
        _ = Throw.IfNull(function);
        return (JsonElement)_functionSchemaCache.GetValue(function, _functionSchemaCreateValueCallback);
    }

    /// <summary>
    /// Gets or creates a transformed JSON schema for the specified <see cref="ChatResponseFormatJson"/> instance.
    /// </summary>
    /// <param name="responseFormat">The response format whose JSON schema we want to transform.</param>
    /// <returns>The transformed JSON schema corresponding to <see cref="TransformOptions"/>.</returns>
    public JsonElement? GetOrCreateTransformedSchema(ChatResponseFormatJson responseFormat)
    {
        _ = Throw.IfNull(responseFormat);
        return responseFormat.Schema is not null
            ? (JsonElement?)_responseFormatCache.GetValue(responseFormat, _responseFormatCreateValueCallback)
            : null;
    }
}
