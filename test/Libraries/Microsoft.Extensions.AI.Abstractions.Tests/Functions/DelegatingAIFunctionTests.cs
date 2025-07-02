// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingAIFunctionTests
{
    [Fact]
    public void DelegatesToInnerMembers()
    {
        AIFunction expected = AIFunctionFactory.Create(() => 42);
        AIFunction actual = new DerivedFunction(expected);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.JsonSchema, actual.JsonSchema);
        Assert.Equal(expected.ReturnJsonSchema, actual.ReturnJsonSchema);
        Assert.Same(expected.JsonSerializerOptions, actual.JsonSerializerOptions);
        Assert.Same(expected.UnderlyingMethod, actual.UnderlyingMethod);
        Assert.Same(expected.AdditionalProperties, actual.AdditionalProperties);
        Assert.Equal(expected.ToString(), actual.ToString());
    }

    [Fact]
    public void AllVirtualsOverridden()
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

    private sealed class DerivedFunction(AIFunction innerFunction) : DelegatingAIFunction(innerFunction);
}
