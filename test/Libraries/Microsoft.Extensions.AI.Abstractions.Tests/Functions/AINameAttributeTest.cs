// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable S1144 // Unused private types or members should be removed (methods are invoked via reflection)

namespace Microsoft.Extensions.AI;

public class AINameAttributeTest
{
    [Fact]
    public void OverridesSchemaPropertyName()
    {
        AIFunction func = AIFunctionFactory.Create(
            ([AIName("my_param")] string myParam, int top) => myParam + top);

        JsonElement expectedSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "my_param": { "type": "string" },
                "top": { "type": "integer" }
            },
            "required": ["my_param", "top"]
        }
        """).RootElement;

        AssertExtensions.EqualJsonValues(expectedSchema, func.JsonSchema);
    }

    [Fact]
    public void HonoredByCreateFunctionJsonSchema()
    {
        Func<string, string> query = ([AIName("my_param")] string myParam) => myParam;

        JsonElement schema = AIJsonUtilities.CreateFunctionJsonSchema(query.Method);

        Assert.Contains("my_param", schema.ToString());
    }

    [Fact]
    public async Task BindsArgumentByOverriddenName_Async()
    {
        AIFunction func = AIFunctionFactory.Create(
            ([AIName("$select")] string select,
            [AIName("$expand")] string expand,
            string filter) =>
                $"select='{select}', expand='{expand}', filter='{filter}'");

        object? result = await func.InvokeAsync(new()
        {
            ["$select"] = "Name,Id",
            ["$expand"] = "Orders",
            ["filter"] = "Active",
        });

        AssertExtensions.EqualFunctionCallResults("select='Name,Id', expand='Orders', filter='Active'", result);
    }

    [Fact]
    public async Task MissingRequiredArgument_ReportsSchemaName_Async()
    {
        AIFunction func = AIFunctionFactory.Create(
            ([AIName("my_param")] string myParam) => myParam);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() => func.InvokeAsync().AsTask());

        Assert.Contains("my_param", ex.Message);
    }

    [Fact]
    public async Task HonoredByStrictUnmappedMemberHandling_Async()
    {
        JsonSerializerOptions strictOptions = new(AIJsonUtilities.DefaultOptions)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        AIFunction func = AIFunctionFactory.Create(
            ([AIName("my_param")] string myParam) => myParam,
            new AIFunctionFactoryOptions { SerializerOptions = strictOptions });

        // The overridden name is "expected", so it passes strict validation.
        AssertExtensions.EqualFunctionCallResults("Name", await func.InvokeAsync(new() { ["my_param"] = "Name" }));

        // The underlying C# name is now an unexpected argument.
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>("arguments", async () =>
            await func.InvokeAsync(new() { ["myParam"] = "Name" }));
        Assert.Contains("myParam", ex.Message);
    }

    [Fact]
    public async Task InheritedByOverride_Async()
    {
        MethodInfo overrideMethod = typeof(MyDerivedType).GetMethod(nameof(MyDerivedType.Method))!;
        AIFunction func = AIFunctionFactory.Create(overrideMethod, new MyDerivedType());

        Assert.Contains("my_param", func.JsonSchema.ToString());
        Assert.DoesNotContain("\"myParam\"", func.JsonSchema.ToString());

        AssertExtensions.EqualFunctionCallResults("param='Name'", await func.InvokeAsync(new() { ["my_param"] = "Name" }));
    }

    [Fact]
    public void OverridesFunctionName()
    {
        AIFunction func = AIFunctionFactory.Create([AIName("my_tool")] () => "result");

        Assert.Equal("my_tool", func.Name);
    }

    [Fact]
    public void OverridesFunctionName_TakesPrecedenceOverDisplayName()
    {
        AIFunction func = AIFunctionFactory.Create([AIName("my_tool")][DisplayName("from-display-name")] () => "result");

        Assert.Equal("my_tool", func.Name);
    }

    [Fact]
    public void DisplayNameAttribute_StillHonoredWhenNoAINameAttribute()
    {
        AIFunction func = AIFunctionFactory.Create([DisplayName("from-display-name")] () => "result");

        Assert.Equal("from-display-name", func.Name);
    }

    [Fact]
    public void OptionsName_TakesPrecedenceOverAttribute()
    {
        AIFunction func = AIFunctionFactory.Create([AIName("my_tool")] () => "result", new AIFunctionFactoryOptions { Name = "explicit" });

        Assert.Equal("explicit", func.Name);
    }

    [Fact]
    public async Task FunctionAndParameterNames_BothHonored_Async()
    {
        AIFunction func = AIFunctionFactory.Create([AIName("my_tool")] ([AIName("my_param")] string myParam) => myParam);

        Assert.Equal("my_tool", func.Name);
        Assert.Contains("my_param", func.JsonSchema.ToString());

        AssertExtensions.EqualFunctionCallResults("Name", await func.InvokeAsync(new() { ["my_param"] = "Name" }));
    }

    [Fact]
    public void NameAndParameterSchema_PreservedByAsDeclarationOnly()
    {
        AIFunction func = AIFunctionFactory.Create([AIName("my_tool")] ([AIName("my_param")] string myParam) => myParam);

        AIFunctionDeclaration declaration = func.AsDeclarationOnly();

        Assert.Equal("my_tool", declaration.Name);
        Assert.Equal(func.JsonSchema.ToString(), declaration.JsonSchema.ToString());
        Assert.Contains("my_param", declaration.JsonSchema.ToString());
        Assert.IsNotAssignableFrom<AIFunction>(declaration);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>("name", () => new AINameAttribute(null!));
        Assert.Throws<ArgumentException>("name", () => new AINameAttribute("   "));
    }

    [Fact]
    public void EscapesNameInJsonPointerRef()
    {
        JsonSerializerOptions options = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        AIFunction func = AIFunctionFactory.Create(
            ([AIName("a/b~c")] RecursiveNode node) => node.ToString(),
            new AIFunctionFactoryOptions { SerializerOptions = options });

        string schema = func.JsonSchema.ToString();

        Assert.Contains("#/properties/a~1b~0c", schema);
        Assert.DoesNotContain("#/properties/a/b~c", schema);
    }

    [Fact]
    public void DuplicateNames_Throw()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => AIFunctionFactory.Create(
            ([AIName("dup")] string first, [AIName("dup")] string second) => first + second));
        Assert.Contains("dup", ex.Message);

        ArgumentException ex2 = Assert.Throws<ArgumentException>(() => AIFunctionFactory.Create(
            ([AIName("filter")] string select, string filter) => select + filter));
        Assert.Contains("filter", ex2.Message);
    }

    private abstract class MyBaseType
    {
        public abstract string Method([AIName("my_param")] string myParam);
    }

    private sealed class MyDerivedType : MyBaseType
    {
        public override string Method(string myParam) => $"param='{myParam}'";
    }

    private sealed class RecursiveNode
    {
        public RecursiveNode? Next { get; set; }
    }
}
