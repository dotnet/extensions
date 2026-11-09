// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System.Runtime.CompilerServices;
using Microsoft.Extensions.VectorData;
using Xunit;

namespace VectorData.UnitTests;

public class TypeForwardingTests
{
    [Fact]
    public void IsExternalInit_IsForwardedToTheRuntime()
    {
        Assert.Contains(typeof(IsExternalInit), typeof(VectorStoreMetadata).Assembly.GetForwardedTypes());
    }
}
#endif
