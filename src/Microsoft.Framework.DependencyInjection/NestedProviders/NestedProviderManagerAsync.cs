// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Framework.DependencyInjection.NestedProviders
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
