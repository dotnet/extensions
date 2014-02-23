using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.DependencyInjection.NestedProviders
{
    public class NestedProviderManager<T> : INestedProviderManager<T>
    {
        private readonly INestedProvider<T>[] _syncProviders;

        public NestedProviderManager(IEnumerable<INestedProvider<T>> providers)
        {
            _syncProviders = providers.OrderBy(p => p.Order).ToArray();
        }

        public void Invoke(T context)
        {
            var caller = new CallNext(context, _syncProviders);

            caller.CallNextProvider();
        }

        private class CallNext
        {
            private readonly T _context;
            private readonly INestedProvider<T>[] _providers;
            private readonly Action _next;

            private int _index;

            public CallNext(T context, INestedProvider<T>[] providers)
            {
                _context = context;
                _next = CallNextProvider;
                _providers = providers;
            }

            public void CallNextProvider()
            {
                if (_providers.Length > _index)
                {
                    _providers[_index++].Invoke(_context, _next);
                }
            }
        }
    }

}
