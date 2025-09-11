// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI.Functions;

public class ApprovalRequiredAIFunctionTests
{
    [Fact]
    public void Constructor_NullFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("innerFunction", () => new ApprovalRequiredAIFunction(null!));
    }

    [Fact]
    public void DelegatesToInnerFunction_Properties()
    {
        var inner = AIFunctionFactory.Create(() => 42);
        var func = new ApprovalRequiredAIFunction(inner);

        Assert.Equal(inner.Name, func.Name);
        Assert.Equal(inner.Description, func.Description);
        Assert.Equal(inner.JsonSchema, func.JsonSchema);
        Assert.Equal(inner.ReturnJsonSchema, func.ReturnJsonSchema);
        Assert.Same(inner.JsonSerializerOptions, func.JsonSerializerOptions);
        Assert.Same(inner.UnderlyingMethod, func.UnderlyingMethod);
        Assert.Same(inner.AdditionalProperties, func.AdditionalProperties);
        Assert.Equal(inner.ToString(), func.ToString());
    }

    [Fact]
    public async Task InvokeAsync_DelegatesToInnerFunction()
    {
        var inner = AIFunctionFactory.Create(() => "result");
        var func = new ApprovalRequiredAIFunction(inner);

        var result = await func.InvokeAsync();

        Assert.Equal("result", result?.ToString());
    }
}
