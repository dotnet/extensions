using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.NestedProviders
{
    public class NestedProviderManagerAsync<T> : INestedProviderManagerAsync<T>
    {
        private readonly INestedProviderAsync<T>[] _asyncProviders;

        public NestedProviderManagerAsync(IEnumerable<INestedProviderAsync<T>> asyncProviders)
        {
            _asyncProviders = asyncProviders.OrderBy(p => p.Order).ToArray();
        }

        public async Task InvokeAsync(T context)
        {
            var caller = new CallNextAsync(context, _asyncProviders);

            await caller.CallNextProvider();
        }

        private class CallNextAsync
        {
            private readonly T _context;
            private readonly INestedProviderAsync<T>[] _providers;
            private readonly Func<Task> _next;

            private int _index;

            public CallNextAsync(T context, INestedProviderAsync<T>[] providers)
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
