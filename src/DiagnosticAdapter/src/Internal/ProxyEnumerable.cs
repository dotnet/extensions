// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyEnumerable<TSourceElement, TTargetElement> : IEnumerable<TTargetElement>
    {
        private readonly IEnumerable<TSourceElement> _source;
        private readonly Type _proxyType;

        public ProxyEnumerable(IEnumerable<TSourceElement> source, Type proxyType)
        {
            _source = source;
            _proxyType = proxyType;
        }

        public IEnumerator<TTargetElement> GetEnumerator()
        {
            return new ProxyEnumerator(_source.GetEnumerator(), _proxyType);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class ProxyEnumerator : IEnumerator<TTargetElement>
        {
            private readonly IEnumerator<TSourceElement> _source;
            private readonly Type _proxyType;

            public ProxyEnumerator(IEnumerator<TSourceElement> source, Type proxyType)
            {
                _source = source;

                _proxyType = proxyType;
            }

            public TTargetElement Current
            {
                get
                {
                    var element = _source.Current;
                    return MakeProxy(element);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                _source.Dispose();
            }

            public bool MoveNext()
            {
                return _source.MoveNext();
            }

            public void Reset()
            {
                _source.Reset();
            }

            private TTargetElement MakeProxy(TSourceElement element)
            {
                if (_proxyType == null)
                {
                    return (TTargetElement)(object)element;
                }
                else if (element == null)
                {
                    return default(TTargetElement);
                }
                else
                {
                    return (TTargetElement)Activator.CreateInstance(
                        _proxyType,
                        new object[] { element });
                }
            }
        }
    }
}
