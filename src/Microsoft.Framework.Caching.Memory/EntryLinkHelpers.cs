// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if DNXCORE50
using System.Threading;
#elif NET45 || DNX451 || DNXCORE50
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

namespace Microsoft.Framework.Caching.Memory
{
    internal static class EntryLinkHelpers
    {
#if DNXCORE50
        private static readonly AsyncLocal<EntryLink> _contextLink = new AsyncLocal<EntryLink>();

        public static EntryLink ContextLink
        {
            get { return _contextLink.Value; }
            set { _contextLink.Value = value; }
        }
#elif NET45 || DNX451
        private const string ContextLinkDataName = "klr.host.EntryLinkHelpers.ContextLink";

        public static EntryLink ContextLink
        {
            get
            {
                var handle = CallContext.LogicalGetData(ContextLinkDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as EntryLink;
            }
            set
            {
                CallContext.LogicalSetData(ContextLinkDataName, new ObjectHandle(value));
            }
        }
#else
        public static EntryLink ContextLink
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }
#endif

        internal static IEntryLink CreateLinkingScope()
        {
            var parentLink = ContextLink;
            var newLink = new EntryLink(parent: parentLink);
            ContextLink = newLink;
            return newLink;
        }

        internal static void DisposeLinkingScope()
        {
            var currentLink = ContextLink;
            var priorLink = ((EntryLink)currentLink).Parent;
            ContextLink = priorLink;
        }
    }
}
