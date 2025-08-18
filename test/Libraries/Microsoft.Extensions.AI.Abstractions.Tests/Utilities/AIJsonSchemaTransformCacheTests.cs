// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Utilities;

public static class AIJsonSchemaTransformCacheTests
{
    [Fact]
    public static void NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AIJsonSchemaTransformCache(transformOptions: null!));
    }

    [Fact]
    public static void EmptyOptions_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AIJsonSchemaTransformCache(transformOptions: new()));
    }

    [Fact]
    public static void TransformOptions_ReturnsExpectedValue()
    {
        AIJsonSchemaTransformOptions options = new() { ConvertBooleanSchemas = true };
        AIJsonSchemaTransformCache cache = new(options);
        Assert.Same(options, cache.TransformOptions);
    }

    [Fact]
    public static void NullFunction_ThrowsArgumentNullException()
    {
        AIJsonSchemaTransformCache cache = new(new() { ConvertBooleanSchemas = true });
        Assert.Throws<ArgumentNullException>(() => cache.GetOrCreateTransformedSchema(function: null!));
    }

    [Fact]
    public static void NullResponseFormat_ThrowsArgumentNullException()
    {
        AIJsonSchemaTransformCache cache = new(new() { ConvertBooleanSchemas = true });
        Assert.Throws<ArgumentNullException>(() => cache.GetOrCreateTransformedSchema(responseFormat: null!));
    }

    [Fact]
    public static void FunctionSchema_ReturnsExpectedResults()
    {
        AIJsonSchemaTransformCache cache = new(new() { TransformSchemaNode = (_, node) => { node.AsObject().Add("myAwesomeKeyword", 42); return node; } });

        AIFunction func = AIFunctionFactory.Create((int x, int y) => x + y);
        JsonElement transformedSchema = cache.GetOrCreateTransformedSchema(func);
        Assert.True(transformedSchema.TryGetProperty("myAwesomeKeyword", out _));

        JsonElement transformedSchema2 = cache.GetOrCreateTransformedSchema(func);
        Assert.Equal(transformedSchema, transformedSchema2);
    }

    [Fact]
    public static void ChatResponseFormat_ReturnsExpectedResults()
    {
        AIJsonSchemaTransformCache cache = new(new() { TransformSchemaNode = (_, node) => { node.AsObject().Add("myAwesomeKeyword", 42); return node; } });

        JsonElement schema = JsonDocument.Parse("{}").RootElement;
        ChatResponseFormatJson responseFormat = ChatResponseFormat.ForJsonSchema(schema);
        JsonElement? transformedSchema = cache.GetOrCreateTransformedSchema(responseFormat);
        Assert.NotNull(transformedSchema);
        Assert.True(transformedSchema.Value.TryGetProperty("myAwesomeKeyword", out _));

        JsonElement? transformedSchema2 = cache.GetOrCreateTransformedSchema(responseFormat);
        Assert.Equal(transformedSchema, transformedSchema2);
    }

    [Fact]
    public static void ChatResponseFormat_NullFormatReturnsNullSchema()
    {
        AIJsonSchemaTransformCache cache = new(new() { TransformSchemaNode = (_, node) => { node.AsObject().Add("myAwesomeKeyword", 42); return node; } });
        JsonElement? transformedSchema = cache.GetOrCreateTransformedSchema(ChatResponseFormat.Json);
        Assert.Null(transformedSchema);
    }
}
