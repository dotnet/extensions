// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
#if !NET9_0_OR_GREATER
using System.Xml.Linq;
#endif
using Xunit;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable xUnit1000 // Test classes must be public

namespace Microsoft.Extensions.AI.JsonSchemaExporter;

public abstract class JsonSchemaExporterTests
{
    protected abstract JsonSerializerOptions Options { get; }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestData), MemberType = typeof(TestTypes))]
    public void TestTypes_GeneratesExpectedJsonSchema(ITestData testData)
    {
        JsonSerializerOptions options = testData.Options is { } opts
            ? new(opts) { TypeInfoResolver = Options.TypeInfoResolver }
            : Options;

        JsonNode schema = options.GetJsonSchemaAsNode(testData.Type, (JsonSchemaExporterOptions?)testData.ExporterOptions);
        Helpers.AssertValidJsonSchema(testData.Type, testData.ExpectedJsonSchema, schema);
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestDataUsingAllValues), MemberType = typeof(TestTypes))]
    public void TestTypes_SerializedValueMatchesGeneratedSchema(ITestData testData)
    {
        JsonSerializerOptions options = testData.Options is { } opts
            ? new(opts) { TypeInfoResolver = Options.TypeInfoResolver }
            : Options;

        JsonNode schema = options.GetJsonSchemaAsNode(testData.Type, (JsonSchemaExporterOptions?)testData.ExporterOptions);
        JsonNode? instance = JsonSerializer.SerializeToNode(testData.Value, testData.Type, options);
        Helpers.AssertDocumentMatchesSchema(schema, instance);
    }

    [Theory]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(int[]), "array")]
    [InlineData(typeof(Dictionary<string, int>), "object")]
    [InlineData(typeof(TestTypes.SimplePoco), "object")]
    public void TreatNullObliviousAsNonNullable_True_MarksAllReferenceTypesAsNonNullable(Type referenceType, string expectedType)
    {
        Assert.True(!referenceType.IsValueType);
        var config = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
        JsonNode schema = Options.GetJsonSchemaAsNode(referenceType, config);
        JsonValue type = Assert.IsAssignableFrom<JsonValue>(schema["type"]);
        Assert.Equal(expectedType, (string)type!);
    }

    [Theory]
    [InlineData(typeof(int), "integer")]
    [InlineData(typeof(double), "number")]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(ImmutableArray<int>), "array")]
    [InlineData(typeof(TestTypes.StructDictionary<string, int>), "object")]
    [InlineData(typeof(TestTypes.SimpleRecordStruct), "object")]
    public void TreatNullObliviousAsNonNullable_True_DoesNotImpactNonReferenceTypes(Type referenceType, string expectedType)
    {
        Assert.True(referenceType.IsValueType);
        var config = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
        JsonNode schema = Options.GetJsonSchemaAsNode(referenceType, config);
        JsonValue value = Assert.IsAssignableFrom<JsonValue>(schema["type"]);
        Assert.Equal(expectedType, (string)value!);
    }

#if !NET9_0 // Disable until https://github.com/dotnet/runtime/pull/108764 gets backported
    [Fact]
    public void CanGenerateXElementSchema()
    {
        JsonNode schema = Options.GetJsonSchemaAsNode(typeof(XElement));
        Assert.True(schema.ToJsonString().Length < 100_000);
    }
#endif

    [Fact]
    public void TreatNullObliviousAsNonNullable_True_DoesNotImpactObjectType()
    {
        var config = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
        JsonNode schema = Options.GetJsonSchemaAsNode(typeof(object), config);
        Assert.False(schema is JsonObject jObj && jObj.ContainsKey("type"));
    }

    [Fact]
    public void TypeWithDisallowUnmappedMembers_AdditionalPropertiesFailValidation()
    {
        JsonNode schema = Options.GetJsonSchemaAsNode(typeof(TestTypes.PocoDisallowingUnmappedMembers));
        JsonNode? jsonWithUnmappedProperties = JsonNode.Parse("""{ "UnmappedProperty" : {} }""");
        Helpers.AssertDoesNotMatchSchema(schema, jsonWithUnmappedProperties);
    }

    [Fact]
    public void GetJsonSchema_NullInputs_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).GetJsonSchemaAsNode(typeof(int)));
        Assert.Throws<ArgumentNullException>(() => Options.GetJsonSchemaAsNode(type: null!));
        Assert.Throws<ArgumentNullException>(() => ((JsonTypeInfo)null!).GetJsonSchemaAsNode());
    }

    [Fact]
    public void GetJsonSchema_NoResolver_ThrowInvalidOperationException()
    {
        var options = new JsonSerializerOptions();
        Assert.Throws<InvalidOperationException>(() => options.GetJsonSchemaAsNode(typeof(int)));
    }

    [Fact]
    public void MaxDepth_SetToZero_NonTrivialSchema_ThrowsInvalidOperationException()
    {
        JsonSerializerOptions options = new(Options) { MaxDepth = 1 };
        var ex = Assert.Throws<InvalidOperationException>(() => options.GetJsonSchemaAsNode(typeof(TestTypes.SimplePoco)));
        Assert.Contains("The depth of the generated JSON schema exceeds the JsonSerializerOptions.MaxDepth setting.", ex.Message);
    }

    [Fact]
    public void ReferenceHandlePreserve_Enabled_ThrowsNotSupportedException()
    {
        var options = new JsonSerializerOptions(Options) { ReferenceHandler = ReferenceHandler.Preserve };
        options.MakeReadOnly();

        var ex = Assert.Throws<NotSupportedException>(() => options.GetJsonSchemaAsNode(typeof(TestTypes.SimplePoco)));
        Assert.Contains("ReferenceHandler.Preserve", ex.Message);
    }
}

public sealed class ReflectionJsonSchemaExporterTests : JsonSchemaExporterTests
{
    protected override JsonSerializerOptions Options => JsonSerializerOptions.Default;
}

public sealed class SourceGenJsonSchemaExporterTests : JsonSchemaExporterTests
{
    protected override JsonSerializerOptions Options => TestTypes.TestTypesContext.Default.Options;
}
