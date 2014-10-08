// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.OptionsModel
{
    public class OptionsAccessor<TOptions> : IOptionsAccessor<TOptions> where TOptions : class,new()
    {
        private object _mapLock = new object();
        private Dictionary<string, TOptions> _namedOptions = new Dictionary<string, TOptions>(StringComparer.OrdinalIgnoreCase);
        private IEnumerable<IOptionsAction<TOptions>> _setups;

        public OptionsAccessor(IEnumerable<IOptionsAction<TOptions>> setups)
        {
            _setups = setups;
        }

        public virtual TOptions GetNamedOptions([NotNull] string name)
        {
            if (!_namedOptions.ContainsKey(name))
            {
                lock (_mapLock)
                {
                    if (!_namedOptions.ContainsKey(name))
                    {
                        _namedOptions[name] = Configure(name);
                    }
                }
            }
            return _namedOptions[name];
        }

        public virtual TOptions Configure(string optionsName = "")
        {
            return _setups == null 
                ? new TOptions() 
                // Always apply default setups (no name specified), otherwise filter to actions with the correct name
                : _setups.Where(s => string.IsNullOrEmpty(s.Name) || string.Equals(s.Name, optionsName, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(setup => setup.Order)
                         .ThenBy(setup => setup.Name)
                         .Aggregate(new TOptions(),
                                    (options, setup) =>
                                    {
                                        setup.Invoke(options);
                                        return options;
                                    });
        }

        public virtual TOptions Options
        {
            get
            {
                return GetNamedOptions("");
            }
        }
    }
}