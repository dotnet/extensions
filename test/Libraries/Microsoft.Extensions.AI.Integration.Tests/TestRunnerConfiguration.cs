// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.AI;

public static class TestRunnerConfiguration
{
    public static IConfiguration Instance { get; } = new ConfigurationBuilder()
        .AddUserSecrets<TypeInThisAssembly>()
        .AddEnvironmentVariables()
        .Build();

    private class TypeInThisAssembly;
}
