// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting.Testing.StartupInitialization.Test;

public static class TestResources
{
    public static IConfigurationSection GetSection(TimeSpan timeout)
    {
        StartupInitializationOptions options;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(StartupInitializationOptions)}:{nameof(options.Timeout)}", timeout.ToString() },
            })
            .Build()
            .GetSection($"{nameof(StartupInitializationOptions)}");
    }
}
