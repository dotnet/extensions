// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETSTANDARD1_3 || NETCORE50
#else
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryHelper
    {
#if NETSTANDARD1_3 || NETCORE50
        private static readonly AsyncLocal<Stack<CacheEntry>> _scopes = new AsyncLocal<Stack<CacheEntry>>();

        internal static Stack<CacheEntry> Scopes
        {
            get { return _scopes.Value; }
            set { _scopes.Value = value; }
        }
#else
        private const string CacheEntryDataName = "CacheEntry.Scopes";

        internal static Stack<CacheEntry> Scopes
        {
            get
            {
                var handle = CallContext.LogicalGetData(CacheEntryDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as Stack<CacheEntry>;
            }
            set
            {
                CallContext.LogicalSetData(CacheEntryDataName, new ObjectHandle(value));
            }
        }
#endif

        internal static CacheEntry Current
        {
            get
            {
                if (Scopes != null)
                {
                    if (Scopes.Count > 0)
                    {
                        return Scopes.Peek();
                    }
                }

                return null;
            }
        }

        internal static void EnterScope(CacheEntry entry)
        {
            if (Scopes == null)
            {
                Scopes = new Stack<CacheEntry>();
            }

            Scopes.Push(entry);
        }

        internal static CacheEntry LeaveScope()
        {
            return Scopes.Pop();
        }
    }
}