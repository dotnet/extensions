// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

