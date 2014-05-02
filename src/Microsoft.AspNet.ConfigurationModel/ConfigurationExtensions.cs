// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
#if NET45 || K10
        public static IConfigurationSourceContainer AddIniFile(this IConfigurationSourceContainer configuration, string path)
        {
            configuration.Add(new IniFileConfigurationSource(path));
            return configuration;
        }
#endif

#if NET45
        public static IConfigurationSourceContainer AddCommandLine(this IConfigurationSourceContainer configuration)
        {
            configuration.Add(new CommandLineConfigurationSource());
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
