// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedgings.Helpers;

public sealed class ConfigurationStubFactory
{
    public static IConfiguration Create(Dictionary<string, string?> collection)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(collection)
            .Build();
    }

    public static IConfiguration CreateEmpty()
    {
        return new ConfigurationBuilder().Build();
    }
}
