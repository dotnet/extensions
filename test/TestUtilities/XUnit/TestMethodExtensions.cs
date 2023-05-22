// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.TestUtilities;

public static class TestMethodExtensions
{
    public static string? EvaluateSkipConditions(this ITestMethod testMethod)
    {
        var testClass = testMethod.TestClass.Class;
        var assembly = testMethod.TestClass.TestCollection.TestAssembly.Assembly;
        var conditionAttributes = testMethod.Method
            .GetCustomAttributes(typeof(ITestCondition))
            .Concat(testClass.GetCustomAttributes(typeof(ITestCondition)))
            .Concat(assembly.GetCustomAttributes(typeof(ITestCondition)))
            .OfType<ReflectionAttributeInfo>()
            .Select(attributeInfo => attributeInfo.Attribute);

        foreach (ITestCondition condition in conditionAttributes.OfType<ITestCondition>())
        {
            if (!condition.IsMet)
            {
                return condition.SkipReason;
            }
        }

        return null;
    }
}
