// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Configuration.EnvironmentVariables;

namespace Microsoft.Framework.Configuration
{
    public static class EnvironmentVariablesExtensions
    {
        public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder configuration)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource());
            return configuration;
        }

        public static IConfigurationBuilder AddEnvironmentVariables(
            this IConfigurationBuilder configuration,
            string prefix)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource(prefix));
            return configuration;
        }
    }
}
