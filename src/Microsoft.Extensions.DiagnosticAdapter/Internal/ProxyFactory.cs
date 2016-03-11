// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.DiagnosticAdapter.Infrastructure;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyFactory : IProxyFactory
    {
        private readonly ProxyTypeCache _cache = new ProxyTypeCache();

        public TProxy CreateProxy<TProxy>(object obj)
        {
            if (obj == null)
            {
                return default(TProxy);
            }
            else if (typeof(TProxy).GetTypeInfo().IsAssignableFrom(obj.GetType().GetTypeInfo()))
            {
                return (TProxy)obj;
            }

            var type = ProxyTypeEmitter.GetProxyType(_cache, typeof(TProxy), obj.GetType());
            return (TProxy)Activator.CreateInstance(type, obj);
        }
    }
}
