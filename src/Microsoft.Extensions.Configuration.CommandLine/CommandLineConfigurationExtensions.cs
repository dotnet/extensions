// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Microsoft.Extensions.Configuration
{
    public static class CommandLineConfigurationExtensions
    {
        public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder configurationBuilder, string[] args)
        {
            configurationBuilder.Add(new CommandLineConfigurationProvider(args));
            return configurationBuilder;
        }

        public static IConfigurationBuilder AddCommandLine(
            this IConfigurationBuilder configurationBuilder,
            string[] args,
            IDictionary<string, string> switchMappings)
        {
            configurationBuilder.Add(new CommandLineConfigurationProvider(args, switchMappings));
            return configurationBuilder;
        }
    }
}
