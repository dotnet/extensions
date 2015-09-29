// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Configuration.EnvironmentVariables;

namespace Microsoft.Framework.Configuration
{
    public static class EnvironmentVariablesExtensions
    {
        public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new EnvironmentVariablesConfigurationProvider());
            return configurationBuilder;
        }

        public static IConfigurationBuilder AddEnvironmentVariables(
            this IConfigurationBuilder configurationBuilder,
            string prefix)
        {
            configurationBuilder.Add(new EnvironmentVariablesConfigurationProvider(prefix));
            return configurationBuilder;
        }
    }
}
