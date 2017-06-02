// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConfigurationConsoleLoggerConfigureOptions : ConfigureOptions<ConsoleLoggerOptions>
    {
        public ConfigurationConsoleLoggerConfigureOptions(IConfiguration configuration) : base(configuration.Bind)
        {
        }
    }
}