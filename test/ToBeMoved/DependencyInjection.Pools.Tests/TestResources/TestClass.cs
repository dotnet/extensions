// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection.Pools.Test.TestResources;

public class TestClass : IResettable, ITestClass
{
    public int ResetCalled { get; private set; }
    private readonly TestDependency _testClass;

    public TestClass(TestDependency testClass)
    {
        _testClass = testClass;
    }

    public string ReadMessage() => _testClass.ReadMessage();

    public bool TryReset()
    {
        ResetCalled++;
        return true;
    }
}
