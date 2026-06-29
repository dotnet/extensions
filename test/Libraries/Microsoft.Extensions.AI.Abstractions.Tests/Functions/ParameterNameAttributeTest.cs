// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable S1144 // Unused private types or members should be removed (methods are invoked via reflection)

namespace Microsoft.Extensions.AI;

public class ParameterNameAttributeTest
{
    [Fact]
    public void OverridesSchemaPropertyName()
    {
        AIFunction func = AIFunctionFactory.Create(
            ([ParameterName("$select")] string select, int top) => select + top);

        JsonElement expectedSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "$select": { "type": "string" },
                "top": { "type": "integer" }
            },
            "required": ["$select", "top"]
        }
        """).RootElement;

        AssertExtensions.EqualJsonValues(expectedSchema, func.JsonSchema);
    }

    [Fact]
    public void HonoredByCreateFunctionJsonSchema()
    {
        Func<string, string> query = ([ParameterName("$select")] string select) => select;

        JsonElement schema = AIJsonUtilities.CreateFunctionJsonSchema(query.Method);

        Assert.Contains("$select", schema.ToString());
    }

    [Fact]
    public async Task BindsArgumentByOverriddenName_Async()
    {
        AIFunction func = AIFunctionFactory.Create(
            ([ParameterName("$select")] string select,
            [ParameterName("$expand")] string expand,
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
            ([ParameterName("$expand")] string expand) => expand);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() => func.InvokeAsync().AsTask());

        Assert.Contains("$expand", ex.Message);
    }

    [Fact]
    public async Task HonoredByStrictUnmappedMemberHandling_Async()
    {
        JsonSerializerOptions strictOptions = new(AIJsonUtilities.DefaultOptions)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        AIFunction func = AIFunctionFactory.Create(
            ([ParameterName("$select")] string select) => select,
            new AIFunctionFactoryOptions { SerializerOptions = strictOptions });

        // The overridden name is "expected", so it passes strict validation.
        AssertExtensions.EqualFunctionCallResults("Name", await func.InvokeAsync(new() { ["$select"] = "Name" }));

        // The underlying C# name is now an unexpected argument.
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>("arguments", async () =>
            await func.InvokeAsync(new() { ["select"] = "Name" }));
        Assert.Contains("select", ex.Message);
    }

    [Fact]
    public async Task InheritedByOverride_Async()
    {
        MethodInfo overrideMethod = typeof(DerivedQuery).GetMethod(nameof(DerivedQuery.Query))!;
        AIFunction func = AIFunctionFactory.Create(overrideMethod, new DerivedQuery());

        Assert.Contains("$select", func.JsonSchema.ToString());
        Assert.DoesNotContain("\"select\"", func.JsonSchema.ToString());

        AssertExtensions.EqualFunctionCallResults("select='Name'", await func.InvokeAsync(new() { ["$select"] = "Name" }));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>("name", () => new ParameterNameAttribute(null!));
        Assert.Throws<ArgumentException>("name", () => new ParameterNameAttribute("   "));
    }

    private abstract class BaseQuery
    {
        public abstract string Query([ParameterName("$select")] string selection);
    }

    private sealed class DerivedQuery : BaseQuery
    {
        public override string Query(string selection) => $"select='{selection}'";
    }
}
