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
