// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Testing.Test;

public static class Setup
{
    public static IConfigurationSection GetFakesConfiguration()
    {
        FakeRedactorOptions options;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(FakeRedactorOptions)}:{nameof(options.RedactionFormat)}", "What is it? O_o '{0}'" },
            })
            .Build()
            .GetSection(nameof(FakeRedactorOptions));
    }
}

