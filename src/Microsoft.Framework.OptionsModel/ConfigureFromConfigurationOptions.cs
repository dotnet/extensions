// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.OptionsModel
{
    public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions>
    {
        public ConfigureFromConfigurationOptions([NotNull] IConfiguration config) 
            : base(options => OptionsServices.ReadProperties(options, config))
        {
        }
    }
}