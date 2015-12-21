// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    public class OptionsManager<TOptions> : IOptions<TOptions> where TOptions : class, new()
    {
        private OptionsCache<TOptions> _optionsCache;

        public OptionsManager(IEnumerable<IConfigureOptions<TOptions>> setups)
        {
            _optionsCache = new OptionsCache<TOptions>(setups);
        }

        public virtual TOptions Value
        {
            get
            {
                return _optionsCache.Value;
            }
        }
    }
}