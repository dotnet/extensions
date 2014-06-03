// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
#if NET45 || K10
        public static T Get<T>(this IConfiguration configuration, string key)
        {
            return (T)Convert.ChangeType(configuration.Get(key), typeof(T));
        }
#endif


#if NET45 || K10
        public static IConfigurationSourceContainer AddIniFile(this IConfigurationSourceContainer configuration, string path)
        {
            configuration.Add(new IniFileConfigurationSource(path));
            return configuration;
        }
#endif

        public static IConfigurationSourceContainer AddCommandLine(this IConfigurationSourceContainer configuration, string[] args)
        {
            configuration.Add(new CommandLineConfigurationSource(args));
            return configuration;
        }

        public static IConfigurationSourceContainer AddEnvironmentVariables(this IConfigurationSourceContainer configuration)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource());
            return configuration;
        }

        public static IConfigurationSourceContainer AddEnvironmentVariables(this IConfigurationSourceContainer configuration, string prefix)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource(prefix));
            return configuration;
        }
    }
}
