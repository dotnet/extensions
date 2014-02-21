using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.NestedProviders
{
    public class NestedProviderManager<T> : INestedProviderManagerAsync<T>
    {
        private readonly INestedProvider<T>[] _syncProviders;
        private readonly INestedProviderAsync<T>[] _asyncProviders;

        public NestedProviderManager(IEnumerable<INestedProvider<T>> providers, IEnumerable<INestedProviderAsync<T>> asyncProviders)
        {
            _syncProviders = providers.OrderBy(p => p.Order).ToArray();
            _asyncProviders = asyncProviders.OrderBy(p => p.Order).ToArray();
        }

        public void Invoke(NestedProviderContext<T> context)
        {
            var caller = new CallNext(context, _syncProviders);

            caller.CallNextProvider();
        }

        public async Task InvokeAsync(NestedProviderContext<T> context)
        {
            var caller = new CallNextAsync(context, _asyncProviders);

            await caller.CallNextProvider();
        }

        private class CallNext
        {
            private readonly NestedProviderContext<T> _context;
            private readonly INestedProvider<T>[] _providers;
            private readonly Action _next;

            private int _index;

            public CallNext(NestedProviderContext<T> context, INestedProvider<T>[] providers)
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

        private class CallNextAsync
        {
            private readonly NestedProviderContext<T> _context;
            private readonly INestedProviderAsync<T>[] _providers;
            private readonly Func<Task> _next;

            private int _index;

            public CallNextAsync(NestedProviderContext<T> context, INestedProviderAsync<T>[] providers)
            {
                _context = context;
                _next = CallNextProvider;
                _providers = providers;
            }

            public async Task CallNextProvider()
            {
                if (_providers.Length > _index)
                {
                    await _providers[_index++].InvokeAsync(_context, _next);
                }
            }
        }
    }
}
