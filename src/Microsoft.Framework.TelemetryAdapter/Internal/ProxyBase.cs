// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.TelemetryAdapter.Internal
{
    public abstract class ProxyBase
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
    }
}

