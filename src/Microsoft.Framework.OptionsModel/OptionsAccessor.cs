// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.OptionsModel
{
    public class OptionsAccessor<TOptions> : IOptionsAccessor<TOptions> where TOptions : class,new()
    {
        private object _lock = new object();
        private TOptions _options;
        private IEnumerable<IOptionsSetup<TOptions>> _setups;

        public OptionsAccessor(IEnumerable<IOptionsSetup<TOptions>> setups)
        {
            _setups = setups;
        }

        public virtual TOptions Options
        {
            get
            {
                if (_options == null)
                {
                    lock (_lock)
                    {
                        if (_options == null)
                        {
                            if (_setups == null)
                            {
                                _options = new TOptions();
                            }
                            else
                            {
                                _options = _setups
                                    .OrderBy(setup => setup.Order)
                                    .Aggregate(
                                        new TOptions(),
                                        (options, setup) =>
                                        {
                                            setup.Setup(options);
                                            return options;
                                        });
                            }
                        }
                    }
                }
                return _options;
            }
        }
    }
}