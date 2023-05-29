// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific package")]
public sealed class LinuxUtilizationExtensionsTest
{
    [ConditionalFact]
    public void Throw_Null_When_Registration_Ingredients_Null()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => ((IResourceMonitorBuilder)null!).AddLinuxProvider());
        Assert.Throws<ArgumentNullException>(() => ((IResourceMonitorBuilder)null!).AddLinuxProvider((_) => { }));
        Assert.Throws<ArgumentNullException>(() => ((IResourceMonitorBuilder)null!).AddLinuxProvider((IConfigurationSection)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddResourceMonitoring((b) => b.AddLinuxProvider((IConfigurationSection)null!)));
        Assert.Throws<ArgumentNullException>(() => services.AddResourceMonitoring((b) => b.AddLinuxProvider((Action<LinuxResourceUtilizationProviderOptions>)null!)));
    }
}
