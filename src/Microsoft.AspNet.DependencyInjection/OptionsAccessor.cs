using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection
{
    public class OptionsAccessor<TOptions> : IOptionsAccessor<TOptions> where TOptions : new()
    {
        private object _lock = new object();
        private TOptions _options;
        private IEnumerable<IOptionsSetup<TOptions>> _setups;

        public OptionsAccessor(IEnumerable<IOptionsSetup<TOptions>> setups)
        {
            _setups = setups;
        }

        public TOptions Options
        {
            get
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
                            // Consider: null out setups without creating race condition?
                        }
                    }
                }
                return _options;
            }
        }
    }
}