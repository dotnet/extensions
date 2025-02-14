// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        FunctionCallContent c = new("callId1", "name");

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("name", c.Name);

        Assert.Null(c.Arguments);
        Assert.Null(c.Exception);
    }

    [Fact]
    public void Constructor_ArgumentsRoundtrip()
    {
        Dictionary<string, object?> args = [];

        FunctionCallContent c = new("id", "name", args);

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("name", c.Name);
        Assert.Equal("id", c.CallId);
        Assert.Same(args, c.Arguments);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        FunctionCallContent c = new("callId1", "name");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);

        Assert.Null(c.Arguments);
        AdditionalPropertiesDictionary args = new() { { "key", "value" } };
        c.Arguments = args;
        Assert.Same(args, c.Arguments);

        Assert.Null(c.Exception);
        Exception e = new();
        c.Exception = e;
        Assert.Same(e, c.Exception);
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializableWithException()
    {
        // Arrange
        var ex = new InvalidOperationException("hello", new NullReferenceException("bye"));
        var sut = new FunctionCallContent("callId1", "functionName", new Dictionary<string, object?> { ["key"] = "value" }) { Exception = ex };

        // Act
        var json = JsonSerializer.SerializeToNode(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<FunctionCallContent>(json, TestJsonSerializerContext.Default.Options);

        // Assert
        Assert.NotNull(deserializedSut);
        Assert.Equal("callId1", deserializedSut.CallId);
        Assert.Equal("functionName", deserializedSut.Name);
        Assert.NotNull(deserializedSut.Arguments);
        Assert.Single(deserializedSut.Arguments);
        Assert.Null(deserializedSut.Exception);
    }

    [Fact]
    public async Task AIFunctionFactory_ObjectValues_Converted()
    {
        Dictionary<string, object?> arguments = new()
        {
            ["a"] = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday },
            ["b"] = 123.4M,
            ["c"] = "072c2d93-7cf6-4d0d-aebc-acc51e6ee7ee",
            ["d"] = new ReadOnlyDictionary<string, string>((new Dictionary<string, string>
            {
                ["p1"] = "42",
                ["p2"] = "43",
            })),
        };

        AIFunction function = AIFunctionFactory.Create((DayOfWeek[] a, double b, Guid c, Dictionary<string, string> d) => b, serializerOptions: TestJsonSerializerContext.Default.Options);
        var result = await function.InvokeAsync(arguments);
        AssertExtensions.EqualFunctionCallResults(123.4, result);
    }

    [Fact]
    public async Task AIFunctionFactory_JsonElementValues_ValuesDeserialized()
    {
        Dictionary<string, object?> arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>("""
            {
              "a": ["Monday", "Tuesday", "Wednesday"],
              "b": 123.4,
              "c": "072c2d93-7cf6-4d0d-aebc-acc51e6ee7ee",
              "d": {
                       "property1": "42",
                       "property2": "43",
                       "property3": "44"
                   }
            }
            """, TestJsonSerializerContext.Default.Options)!;
        Assert.All(arguments.Values, v => Assert.IsType<JsonElement>(v));

        AIFunction function = AIFunctionFactory.Create((DayOfWeek[] a, double b, Guid c, Dictionary<string, string> d) => b, serializerOptions: TestJsonSerializerContext.Default.Options);
        var result = await function.InvokeAsync(arguments);
        AssertExtensions.EqualFunctionCallResults(123.4, result);
    }

    [Fact]
    public void AIFunctionFactory_WhenTypesUnknownByContext_Throws()
    {
        var ex = Assert.Throws<NotSupportedException>(() => AIFunctionFactory.Create((CustomType arg) => { }, serializerOptions: TestJsonSerializerContext.Default.Options));
        Assert.Contains("JsonTypeInfo metadata", ex.Message);
        Assert.Contains(nameof(CustomType), ex.Message);

        ex = Assert.Throws<NotSupportedException>(() => AIFunctionFactory.Create(() => new CustomType(), serializerOptions: TestJsonSerializerContext.Default.Options));
        Assert.Contains("JsonTypeInfo metadata", ex.Message);
        Assert.Contains(nameof(CustomType), ex.Message);
    }

    [Fact]
    public async Task AIFunctionFactory_JsonDocumentValues_ValuesDeserialized()
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>("""
            {
              "a": ["Monday", "Tuesday", "Wednesday"],
              "b": 123.4,
              "c": "072c2d93-7cf6-4d0d-aebc-acc51e6ee7ee",
              "d": {
                       "property1": "42",
                       "property2": "43",
                       "property3": "44"
                   }
            }
            """, TestJsonSerializerContext.Default.Options)!.ToDictionary(k => k.Key, k => (object?)k.Value);

        AIFunction function = AIFunctionFactory.Create((DayOfWeek[] a, double b, Guid c, Dictionary<string, string> d) => b, serializerOptions: TestJsonSerializerContext.Default.Options);
        var result = await function.InvokeAsync(arguments);
        AssertExtensions.EqualFunctionCallResults(123.4, result);
    }

    [Fact]
    public async Task AIFunctionFactory_JsonNodeValues_ValuesDeserialized()
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>("""
            {
              "a": ["Monday", "Tuesday", "Wednesday"],
              "b": 123.4,
              "c": "072c2d93-7cf6-4d0d-aebc-acc51e6ee7ee",
              "d": {
                       "property1": "42",
                       "property2": "43",
                       "property3": "44"
                   }
            }
            """, TestJsonSerializerContext.Default.Options)!.ToDictionary(k => k.Key, k => (object?)k.Value);

        AIFunction function = AIFunctionFactory.Create((DayOfWeek[] a, double b, Guid c, Dictionary<string, string> d) => b, serializerOptions: TestJsonSerializerContext.Default.Options);
        var result = await function.InvokeAsync(arguments);
        AssertExtensions.EqualFunctionCallResults(123.4, result);
    }

    [Fact]
    public async Task TypelessAIFunction_JsonDocumentValues_AcceptsArguments()
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>("""
            {
              "a": "string",
              "b": 123.4,
              "c": true,
              "d": false,
              "e": ["Monday", "Tuesday", "Wednesday"],
              "f": null
            }
            """, TestJsonSerializerContext.Default.Options)!.ToDictionary(k => k.Key, k => (object?)k.Value);

        var result = await NetTypelessAIFunction.Instance.InvokeAsync(arguments);
        Assert.Same(result, arguments);
    }

    [Fact]
    public async Task TypelessAIFunction_JsonElementValues_AcceptsArguments()
    {
        Dictionary<string, object?> arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>("""
            {
              "a": "string",
              "b": 123.4,
              "c": true,
              "d": false,
              "e": ["Monday", "Tuesday", "Wednesday"],
              "f": null
            }
            """, TestJsonSerializerContext.Default.Options)!;

        var result = await NetTypelessAIFunction.Instance.InvokeAsync(arguments);
        Assert.Same(result, arguments);
    }

    [Fact]
    public async Task TypelessAIFunction_JsonNodeValues_AcceptsArguments()
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>("""
            {
              "a": "string",
              "b": 123.4,
              "c": true,
              "d": false,
              "e": ["Monday", "Tuesday", "Wednesday"],
              "f": null
            }
            """, TestJsonSerializerContext.Default.Options)!.ToDictionary(k => k.Key, k => (object?)k.Value);

        var result = await NetTypelessAIFunction.Instance.InvokeAsync(arguments);
        Assert.Same(result, arguments);
    }

    private sealed class CustomType;

    private sealed class NetTypelessAIFunction : AIFunction
    {
        public static NetTypelessAIFunction Instance { get; } = new NetTypelessAIFunction();

        public override string Name => "NetTypeless";
        public override string Description => "AIFunction with parameters that lack .NET types";
        protected override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>>? arguments, CancellationToken cancellationToken) =>
            Task.FromResult<object?>(arguments);
    }

    [Fact]
    public static void CreateFromParsedArguments_ObjectJsonInput_ReturnsElementArgumentDictionary()
    {
        var content = FunctionCallContent.CreateFromParsedArguments(
            """{"Key1":{}, "Key2":null, "Key3" : [], "Key4" : 42, "Key5" : true }""",
            "callId",
            "functionName",
            argumentParser: static json => JsonSerializer.Deserialize<Dictionary<string, object?>>(json));

        Assert.NotNull(content);
        Assert.Null(content.Exception);
        Assert.NotNull(content.Arguments);
        Assert.Equal(5, content.Arguments.Count);
        Assert.Collection(content.Arguments,
            kvp =>
            {
                Assert.Equal("Key1", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Object });
            },
            kvp =>
            {
                Assert.Equal("Key2", kvp.Key);
                Assert.Null(kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Key3", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Array });
            },
            kvp =>
            {
                Assert.Equal("Key4", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Number });
            },
            kvp =>
            {
                Assert.Equal("Key5", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.True });
            });
    }

    [Theory]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(NotSupportedException))]
    public static void CreateFromParsedArguments_ParseException_HasExpectedHandling(Type exceptionType)
    {
        var exc = (Exception)Activator.CreateInstance(exceptionType)!;
        var content = FunctionCallContent.CreateFromParsedArguments(exc, "callId", "functionName", ThrowingParser);

        Assert.Equal("functionName", content.Name);
        Assert.Equal("callId", content.CallId);
        Assert.Null(content.Arguments);
        Assert.IsType<InvalidOperationException>(content.Exception);
        Assert.Same(exc, content.Exception.InnerException);

        static Dictionary<string, object?> ThrowingParser(Exception ex) => throw ex;
    }

    [Fact]
    public static void CreateFromParsedArguments_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("encodedArguments", () => FunctionCallContent.CreateFromParsedArguments((string)null!, "callId", "functionName", _ => null));
        Assert.Throws<ArgumentNullException>("callId", () => FunctionCallContent.CreateFromParsedArguments("{}", null!, "functionName", _ => null));
        Assert.Throws<ArgumentNullException>("name", () => FunctionCallContent.CreateFromParsedArguments("{}", "callId", null!, _ => null));
        Assert.Throws<ArgumentNullException>("argumentParser", () => FunctionCallContent.CreateFromParsedArguments("{}", "callId", "functionName", null!));
    }
}
