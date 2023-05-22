// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.StartupInitialization.Test;
public class StartupInitializationExtensionsTest
{
    [Fact]
    public void Public_API_Throws_On_Nulls()
    {
        var s = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => s.AddStartupInitialization((Action<StartupInitializationOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => s.AddStartupInitialization((IConfigurationSection)null!));
        Assert.Throws<ArgumentNullException>(() => s.AddStartupInitialization().AddInitializer(null!));
    }

    [Fact]
    public void Startup_Initializers_Are_Registered_As_Transient_So_They_Do_Not_Waste_Memory_After_They_Are_Used()
    {
        var s = new ServiceCollection()
            .AddLogging()
            .AddStartupInitialization()
            .AddInitializer<DatabaseInitializer>()
            .Services;

        using var sp = s.BuildServiceProvider();

        var first = sp.GetRequiredService<IStartupInitializer>();
        var second = sp.GetRequiredService<IStartupInitializer>();

        Assert.IsAssignableFrom<DatabaseInitializer>(first);
        Assert.IsAssignableFrom<DatabaseInitializer>(second);
        Assert.NotEqual(first, second);
    }
}
