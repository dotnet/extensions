// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.OptionsModel
{
    public class ConfigOptionsSetup<TOptions> : IOptionsSetup<TOptions>
    {
        private readonly IConfiguration _config;

        public ConfigOptionsSetup(IConfiguration config, int order = OptionsConstants.ConfigurationOrder)
        {
            Order = order;
            _config = config;
        }

        public int Order { get; set; }

        public virtual void Setup(TOptions options)
        {
            OptionsServices.ReadProperties(options, _config);
        }
    }
}