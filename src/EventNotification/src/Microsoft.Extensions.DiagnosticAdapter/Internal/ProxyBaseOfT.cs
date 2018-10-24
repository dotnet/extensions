// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyBase<T> : ProxyBase where T : class
    {
        // Used by reflection, don't rename.
        public readonly T Instance;

        public ProxyBase(T instance)
            : base(typeof(T))
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

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

