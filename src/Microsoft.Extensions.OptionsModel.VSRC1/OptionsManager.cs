// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.OptionsModel.VSRC1
{
    public class OptionsManager<TOptions> : IOptions<TOptions> where TOptions : class, new()
    {
        private TOptions _options;
        private IEnumerable<IConfigureOptions<TOptions>> _setups;

        public OptionsManager(IEnumerable<IConfigureOptions<TOptions>> setups)
        {
            _setups = setups;
        }

        public virtual TOptions Value
        {
            get
            {
                if (_options == null)
                {
                    _options = _setups == null
                        ? new TOptions()
                        : _setups.Aggregate(new TOptions(),
                                            (options, setup) =>
                                            {
                                                setup.Configure(options);
                                                return options;
                                            });
                }
                return _options;
            }
        }
    }
}