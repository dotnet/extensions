// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience.Test.Helpers;

public sealed class ConfigurationStubFactory
{
    public static IConfiguration Create(Dictionary<string, string?> collection) => Create(collection, out _);

    public static IConfiguration Create(Dictionary<string, string?> collection, out Action<Dictionary<string, string?>> reload)
    {
        var reloadable = new ReloadableConfiguration();
        reloadable.Reload(collection);

        reload = reloadable.Reload;

        return new ConfigurationBuilder()
            .Add(reloadable)
            .Build();
    }

    public static IConfiguration CreateEmpty()
    {
        return new ConfigurationBuilder().Build();
    }

    private class ReloadableConfiguration : ConfigurationProvider, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public void Reload(Dictionary<string, string?> data)
        {
            Data = new Dictionary<string, string?>(data, StringComparer.OrdinalIgnoreCase);
            OnReload();
        }
    }
}
