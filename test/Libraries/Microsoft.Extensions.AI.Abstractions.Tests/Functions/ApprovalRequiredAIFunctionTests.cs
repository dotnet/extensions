// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI.Functions;

public class ApprovalRequiredAIFunctionTests
{
    private sealed class DummyAIFunction : AIFunction
    {
        public override string Name => "Dummy";
        public override string Description => "A dummy function";
        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
            => new("result");
    }

    [Fact]
    public void Constructor_NullFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ApprovalRequiredAIFunction(null!));
    }

    [Fact]
    public async Task Constructor_SetsDefaultRequiresApprovalCallbackAsync()
    {
        var inner = new DummyAIFunction();
        var func = new ApprovalRequiredAIFunction(inner);

        Assert.NotNull(func.RequiresApprovalCallback);

        var context = new ApprovalRequiredAIFunction.ApprovalContext(new FunctionCallContent("FCC1", "TestFunction", new AIFunctionArguments()));
        var result = await func.RequiresApprovalCallback(context, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task RequiresApprovalCallback_CanBeOverridden()
    {
        var inner = new DummyAIFunction();
        var func = new ApprovalRequiredAIFunction(inner)
        {
            RequiresApprovalCallback = (_, _) => new ValueTask<bool>(false)
        };

        var context = new ApprovalRequiredAIFunction.ApprovalContext(new FunctionCallContent("FCC1", "TestFunction", new AIFunctionArguments()));
        var result = await func.RequiresApprovalCallback(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RequiresApprovalCallback_ReceivesCorrectArguments()
    {
        var functionCallContent = new FunctionCallContent("FCC1", "TestFunction", new AIFunctionArguments());
        var inner = new DummyAIFunction();
        var func = new ApprovalRequiredAIFunction(inner)
        {
            RequiresApprovalCallback = (ctx, ct) => new ValueTask<bool>(ctx.FunctionCall.CallId == "FCC1" && ct == CancellationToken.None)
        };

        var context = new ApprovalRequiredAIFunction.ApprovalContext(functionCallContent);
        var result = await func.RequiresApprovalCallback(context, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public void ApprovalContext_NullFunctionCall_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ApprovalRequiredAIFunction.ApprovalContext(null!));
    }

    [Fact]
    public void ApprovalContext_FunctionCall_Roundtrips()
    {
        var callContent = new FunctionCallContent("FCC1", "TestFunction", new AIFunctionArguments());
        var context = new ApprovalRequiredAIFunction.ApprovalContext(callContent);

        Assert.Same(callContent, context.FunctionCall);
    }

    [Fact]
    public void DelegatesToInnerFunction_Properties()
    {
        var inner = new DummyAIFunction();
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
        var inner = new DummyAIFunction();
        var func = new ApprovalRequiredAIFunction(inner);

        var args = new AIFunctionArguments();
        var result = await func.InvokeAsync(args, CancellationToken.None);

        Assert.Equal("result", result);
    }
}
