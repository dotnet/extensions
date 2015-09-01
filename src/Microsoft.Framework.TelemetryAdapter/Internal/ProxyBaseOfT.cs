// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.Framework.TelemetryAdapter.Internal
{
    public class ProxyBase<T> : ProxyBase where T : class
    {
        // Used by reflection, don't rename.
        public readonly T Instance;

        public ProxyBase([NotNull] T instance)
            : base(typeof(T))
        {
            Instance = instance;
        }

        public T UnderlyingInstance
        {
            get
            {
                return Instance;
            }
        }

        public override object UnderlyingInstanceAsObject
        {
            get
            {
                return Instance;
            }
        }
    }
}

