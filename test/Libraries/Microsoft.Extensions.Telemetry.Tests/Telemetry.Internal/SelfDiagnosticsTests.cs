// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class SelfDiagnosticsTests
{
    [Fact]
    public void SelfDiagnostics_EnsureInitialized_DoesNotThrow()
    {
        var exception = Record.Exception(SelfDiagnostics.EnsureInitialized);
        Assert.Null(exception);
    }

    [Fact]
    public void SelfDiagnostics_DoesNotThrow()
    {
        var sd = new SelfDiagnostics();

        Assert.Null(Record.Exception(() => sd.Dispose()));

        sd.Dispose();
    }

    [Fact]
    public void SelfDiagnostics_OnProcessExit_DoesNotThrow()
    {
        var exception = Record.Exception(() => SelfDiagnostics.OnProcessExit(null, EventArgs.Empty));
        Assert.Null(exception);
    }
}
