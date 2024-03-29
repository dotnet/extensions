// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.ObjectPool.Test.TestResources;

public sealed class TestClass : IResettable, IDisposable, ITestClass
{
    public int ResetCalled { get; private set; }
    public int DisposedCalled { get; private set; }

    public TestClass(TestDependency _)
    {
    }

    public string ReadMessage() => "I'm here!";

    public bool TryReset()
    {
        ResetCalled++;
        return true;
    }

    public void Dispose()
    {
        DisposedCalled++;
    }
}
