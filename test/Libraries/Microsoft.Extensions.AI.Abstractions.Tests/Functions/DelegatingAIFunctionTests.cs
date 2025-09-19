// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingAIFunctionTests
{
    [Fact]
    public void Constructor_NullInnerFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("innerFunction", () => new DerivedFunction(null!));
    }

    [Fact]
    public void DefaultOverrides_DelegateToInnerFunction()
    {
        AIFunction expected = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => 42));
        DerivedFunction actual = new(expected);

        Assert.Same(expected, actual.InnerFunction);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.JsonSchema, actual.JsonSchema);
        Assert.Equal(expected.ReturnJsonSchema, actual.ReturnJsonSchema);
        Assert.Same(expected.JsonSerializerOptions, actual.JsonSerializerOptions);
        Assert.Same(expected.UnderlyingMethod, actual.UnderlyingMethod);
        Assert.Same(expected.AdditionalProperties, actual.AdditionalProperties);
        Assert.Equal(expected.ToString(), actual.ToString());
        Assert.Same(expected, actual.GetService<ApprovalRequiredAIFunction>());
    }

    private sealed class DerivedFunction(AIFunction innerFunction) : DelegatingAIFunction(innerFunction)
    {
        public new AIFunction InnerFunction => base.InnerFunction;
    }

    [Fact]
    public void Virtuals_AllOverridden()
    {
        Assert.All(typeof(DelegatingAIFunction).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), m =>
        {
            switch (m)
            {
                case MethodInfo methodInfo when methodInfo.IsVirtual && methodInfo.Name is not ("Finalize" or "Equals" or "GetHashCode"):
                    Assert.True(methodInfo.DeclaringType == typeof(DelegatingAIFunction), $"{methodInfo.Name} not overridden");
                    break;

                case PropertyInfo propertyInfo when propertyInfo.GetMethod?.IsVirtual is true:
                    Assert.True(propertyInfo.DeclaringType == typeof(DelegatingAIFunction), $"{propertyInfo.Name} not overridden");
                    break;
            }
        });
    }

    [Fact]
    public async Task OverriddenInvocation_SuccessfullyInvoked()
    {
        bool innerInvoked = false;
        AIFunction inner = AIFunctionFactory.Create(int () =>
        {
            innerInvoked = true;
            throw new Exception("uh oh");
        }, "TestFunction", "A test function for DelegatingAIFunction");

        AIFunction actual = new OverridesInvocation(inner, (args, ct) => new ValueTask<object?>(84));

        Assert.Equal(inner.Name, actual.Name);
        Assert.Equal(inner.Description, actual.Description);
        Assert.Equal(inner.JsonSchema, actual.JsonSchema);
        Assert.Equal(inner.ReturnJsonSchema, actual.ReturnJsonSchema);
        Assert.Same(inner.JsonSerializerOptions, actual.JsonSerializerOptions);
        Assert.Same(inner.UnderlyingMethod, actual.UnderlyingMethod);
        Assert.Same(inner.AdditionalProperties, actual.AdditionalProperties);
        Assert.Equal(inner.ToString(), actual.ToString());

        object? result = await actual.InvokeAsync(new(), CancellationToken.None);
        Assert.Contains("84", result?.ToString());

        Assert.False(innerInvoked);
    }

    private sealed class OverridesInvocation(AIFunction innerFunction, Func<AIFunctionArguments, CancellationToken, ValueTask<object?>> invokeAsync) : DelegatingAIFunction(innerFunction)
    {
        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
            invokeAsync(arguments, cancellationToken);
    }
}
