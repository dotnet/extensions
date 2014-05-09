// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.DependencyInjection.NestedProviders
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
