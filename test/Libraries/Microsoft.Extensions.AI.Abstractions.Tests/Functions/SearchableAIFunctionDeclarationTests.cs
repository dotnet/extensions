// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI.Functions;

public class SearchableAIFunctionDeclarationTests
{
    [Fact]
    public void Constructor_NullFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("innerFunction", () => new SearchableAIFunctionDeclaration(null!));
    }

    [Fact]
    public void Constructor_DelegatesToInnerFunction_Properties()
    {
        var inner = AIFunctionFactory.Create(() => 42, "MyFunc", "My description");
        var wrapper = new SearchableAIFunctionDeclaration(inner);

        Assert.Equal(inner.Name, wrapper.Name);
        Assert.Equal(inner.Description, wrapper.Description);
        Assert.Equal(inner.JsonSchema, wrapper.JsonSchema);
        Assert.Equal(inner.ReturnJsonSchema, wrapper.ReturnJsonSchema);
        Assert.Same(inner.AdditionalProperties, wrapper.AdditionalProperties);
        Assert.Equal(inner.ToString(), wrapper.ToString());
    }

    [Fact]
    public void GetService_ReturnsSelf()
    {
        var inner = AIFunctionFactory.Create(() => 42);
        var wrapper = new SearchableAIFunctionDeclaration(inner);

        Assert.Same(wrapper, wrapper.GetService<SearchableAIFunctionDeclaration>());
    }

    [Fact]
    public void CreateToolSet_NullFunctions_Throws()
    {
        Assert.Throws<ArgumentNullException>("functions", () => SearchableAIFunctionDeclaration.CreateToolSet(null!));
    }

    [Fact]
    public void CreateToolSet_ReturnsHostedToolSearchToolFirst_ThenWrappedFunctions()
    {
        var f1 = AIFunctionFactory.Create(() => 1, "F1");
        var f2 = AIFunctionFactory.Create(() => 2, "F2");

        var tools = SearchableAIFunctionDeclaration.CreateToolSet([f1, f2]);

        Assert.Equal(3, tools.Count);
        Assert.IsType<HostedToolSearchTool>(tools[0]);
        Assert.Empty(tools[0].AdditionalProperties);

        var s1 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[1]);
        Assert.Equal("F1", s1.Name);

        var s2 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[2]);
        Assert.Equal("F2", s2.Name);
    }

    [Fact]
    public void CreateToolSet_WithAdditionalProperties_PassesToHostedToolSearchTool()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        var f1 = AIFunctionFactory.Create(() => 1, "F1");

        var tools = SearchableAIFunctionDeclaration.CreateToolSet([f1], toolSearchProperties: props);

        var toolSearch = Assert.IsType<HostedToolSearchTool>(tools[0]);
        Assert.Same(props, toolSearch.AdditionalProperties);
    }

    [Fact]
    public void WrappingApprovalRequired_GetService_ResolvesBothMarkerTypes()
    {
        var inner = AIFunctionFactory.Create(() => 42, "MyFunc", "My description");
        var approval = new ApprovalRequiredAIFunction(inner);
        var searchable = new SearchableAIFunctionDeclaration(approval);

        Assert.Same(searchable, searchable.GetService<SearchableAIFunctionDeclaration>());
        Assert.Same(approval, searchable.GetService<ApprovalRequiredAIFunction>());
    }

    [Fact]
    public void WrappingApprovalRequired_PropertiesDelegateThroughBothLayers()
    {
        var inner = AIFunctionFactory.Create(() => 42, "MyFunc", "My description");
        var approval = new ApprovalRequiredAIFunction(inner);
        var searchable = new SearchableAIFunctionDeclaration(approval);

        Assert.Equal("MyFunc", searchable.Name);
        Assert.Equal("My description", searchable.Description);
        Assert.Equal(inner.JsonSchema, searchable.JsonSchema);
        Assert.Equal(inner.ReturnJsonSchema, searchable.ReturnJsonSchema);
        Assert.Same(inner.AdditionalProperties, searchable.AdditionalProperties);
    }

    [Fact]
    public void CreateToolSet_WithApprovalRequiredFunctions_PreservesBothMarkers()
    {
        var f1 = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => 1, "F1"));
        var f2 = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => 2, "F2"));

        var tools = SearchableAIFunctionDeclaration.CreateToolSet([f1, f2]);

        Assert.Equal(3, tools.Count);
        Assert.IsType<HostedToolSearchTool>(tools[0]);

        var s1 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[1]);
        Assert.Equal("F1", s1.Name);
        Assert.NotNull(s1.GetService<ApprovalRequiredAIFunction>());

        var s2 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[2]);
        Assert.Equal("F2", s2.Name);
        Assert.NotNull(s2.GetService<ApprovalRequiredAIFunction>());
    }
}
