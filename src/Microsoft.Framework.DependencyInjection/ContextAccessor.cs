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

namespace Microsoft.Framework.DependencyInjection
{
    public class ContextAccessor<TContext> : IContextAccessor<TContext>
    {
        private ContextSource _source;
        private TContext _value;

        public ContextAccessor()
        {
            _source = new ContextSource();
        }

        public TContext Value
        {
            get
            {
                return _source.Access != null ? _source.Access() : _value;
            }
        }

        public TContext SetValue(TContext value)
        {
            if (_source.Exchange != null)
            {
                return _source.Exchange(value);
            }
            var prior = _value;
            _value = value;
            return prior;
        }

        public IDisposable SetContextSource(Func<TContext> access, Func<TContext, TContext> exchange)
        {
            var prior = _source;
            _source = new ContextSource(access, exchange);
            return new Disposable(this, prior);
        }

        struct ContextSource
        {
            public ContextSource(Func<TContext> access, Func<TContext, TContext> exchange)
            {
                Access = access;
                Exchange = exchange;
            }

            public readonly Func<TContext> Access;
            public readonly Func<TContext, TContext> Exchange;
        }

        class Disposable : IDisposable
        {
            private readonly ContextAccessor<TContext> _contextAccessor;
            private readonly ContextSource _source;

            public Disposable(ContextAccessor<TContext> contextAccessor, ContextSource source)
            {
                _contextAccessor = contextAccessor;
                _source = source;
            }

            public void Dispose()
            {
                _contextAccessor._source = _source;
            }
        }
    }
}