// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.CommandLine
{
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        public IDictionary<string, string> SwitchMappings { get; set; }

        public IEnumerable<string> Args { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CommandLineConfigurationProvider(Args, SwitchMappings);
        }
    }
}
