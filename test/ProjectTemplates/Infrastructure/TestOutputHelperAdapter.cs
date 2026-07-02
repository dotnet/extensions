// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Shared.ProjectTemplates.Tests;

/// <summary>
/// Adapts xUnit v3's <see cref="Xunit.ITestOutputHelper"/> to xUnit v2's
/// <see cref="Xunit.Abstractions.ITestOutputHelper"/> for use with libraries
/// that still depend on the v2 interface.
/// </summary>
internal sealed class TestOutputHelperAdapter : Xunit.Abstractions.ITestOutputHelper
{
    private readonly Xunit.ITestOutputHelper _inner;

    public TestOutputHelperAdapter(Xunit.ITestOutputHelper inner)
    {
        _inner = inner;
    }

    public void WriteLine(string message) => _inner.WriteLine(message);

    public void WriteLine(string format, params object[] args) => _inner.WriteLine(format, args);
}
