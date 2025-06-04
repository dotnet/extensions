// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
internal sealed class CollectionBuilderAttribute : Attribute
{
    public CollectionBuilderAttribute(Type builderType, string methodName)
    {
        BuilderType = builderType;
        MethodName = methodName;
    }

    public Type BuilderType { get; }
    public string MethodName { get; }
}


#endif
