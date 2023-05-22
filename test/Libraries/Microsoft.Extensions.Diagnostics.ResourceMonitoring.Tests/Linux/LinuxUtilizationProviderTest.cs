// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

public sealed class LinuxUtilizationProviderTest
{
    [Fact]
    public void Null_Checks()
    {
        Assert.Throws<ArgumentException>(() => new LinuxUtilizationProvider(Microsoft.Extensions.Options.Options.Create<LinuxResourceUtilizationProviderOptions>(null!), null!, null!, null!, null!));
        Assert.Throws<ArgumentException>(() => new LinuxUtilizationProvider(null!, null!, null!, null!));
    }
}
