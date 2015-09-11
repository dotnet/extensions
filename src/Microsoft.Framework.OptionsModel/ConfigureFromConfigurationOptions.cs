// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Configuration;

namespace Microsoft.Framework.OptionsModel
{
    public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions>
        where TOptions : class
    {
        public ConfigureFromConfigurationOptions(IConfiguration config)
            : base(options => ConfigurationBinder.Bind(config, options))
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
        }
    }
}