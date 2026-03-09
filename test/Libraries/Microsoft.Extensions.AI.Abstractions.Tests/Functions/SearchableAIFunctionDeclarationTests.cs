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
    public void Namespace_DefaultIsNull()
    {
        var inner = AIFunctionFactory.Create(() => 42);
        var wrapper = new SearchableAIFunctionDeclaration(inner);

        Assert.Null(wrapper.Namespace);
    }

    [Fact]
    public void Namespace_Roundtrips()
    {
        var inner = AIFunctionFactory.Create(() => 42);
        var wrapper = new SearchableAIFunctionDeclaration(inner, namespaceName: "myNamespace");

        Assert.Equal("myNamespace", wrapper.Namespace);
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
        Assert.Null(s1.Namespace);

        var s2 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[2]);
        Assert.Equal("F2", s2.Name);
        Assert.Null(s2.Namespace);
    }

    [Fact]
    public void CreateToolSet_WithNamespaceAndProperties_Roundtrips()
    {
        var f1 = AIFunctionFactory.Create(() => 1, "F1");
        var props = new Dictionary<string, object?> { ["key"] = "value" };

        var tools = SearchableAIFunctionDeclaration.CreateToolSet([f1], namespaceName: "ns", toolSearchProperties: props);

        Assert.Equal(2, tools.Count);

        var hostTool = Assert.IsType<HostedToolSearchTool>(tools[0]);
        Assert.Same(props, hostTool.AdditionalProperties);

        var s1 = Assert.IsType<SearchableAIFunctionDeclaration>(tools[1]);
        Assert.Equal("ns", s1.Namespace);
    }
}
