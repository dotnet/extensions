// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.OptionsModel
{

    public class ConfigurationAction<TOptions> : OptionsAction<TOptions>
    {
        public ConfigurationAction([NotNull] IConfiguration config) 
            : base(options => OptionsServices.ReadProperties(options, config))
        {
        }
    }
}