// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Configuration;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.OptionsModel
{
    public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions>
    {
        public ConfigureFromConfigurationOptions([NotNull] IConfiguration config)
            : base(options => ConfigurationBinder.Bind(options, config))
        {
        }
    }
}