// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

