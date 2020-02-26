// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DiagnosticAdapter.Infrastructure;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public abstract class ProxyBase : IProxy
    {
        public readonly Type WrappedType;

        protected ProxyBase(Type wrappedType)
        {
            WrappedType = wrappedType;
        }

        // Used by reflection, don't rename.
        public abstract object UnderlyingInstanceAsObject
        {
            get;
        }

        public T Upwrap<T>()
        {
            return (T)UnderlyingInstanceAsObject;
        }
    }
}

