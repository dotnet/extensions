// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Shared.Diagnostics.Test;

public class DebuggerTest
{
    [Fact]
    public void Debugger_Classes_Return_Booleans()
    {
        Assert.False(DebuggerState.System.IsAttached);
        Assert.True(DebuggerState.Attached.IsAttached);
        Assert.False(DebuggerState.Detached.IsAttached);
    }

    [Fact]
    public void System_Debugger_From_Service_Collection_Is_Detached()
    {
        using var provider = new ServiceCollection()
            .AddSystemDebuggerState()
            .BuildServiceProvider();

        var debugger = provider.GetRequiredService<IDebuggerState>();

        Assert.IsAssignableFrom<IDebuggerState>(debugger);
        Assert.False(debugger.IsAttached);
    }

    [Fact]
    public void Detached_Debugger_From_Service_Collection_Is_Detached()
    {
        using var provider = new ServiceCollection()
            .AddDetachedDebuggerState()
            .BuildServiceProvider();

        var debugger = provider.GetRequiredService<IDebuggerState>();

        Assert.IsAssignableFrom<IDebuggerState>(debugger);
        Assert.False(debugger.IsAttached);
    }

    [Fact]
    public void Attached_Debugger_From_Service_Collection_Is_Attached()
    {
        using var provider = new ServiceCollection()
            .AddAttachedDebuggerState()
            .BuildServiceProvider();

        var debugger = provider.GetRequiredService<IDebuggerState>();

        Assert.IsAssignableFrom<IDebuggerState>(debugger);
        Assert.True(debugger.IsAttached);
    }

    [Fact]
    public void Debugger_Extensions_Does_Not_Allow_Nulls()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddAttachedDebuggerState());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddDetachedDebuggerState());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddSystemDebuggerState());
    }
}
