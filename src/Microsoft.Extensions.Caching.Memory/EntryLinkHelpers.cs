// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DOTNET5_4 || NETCORE50
using System.Threading;
#else
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

namespace Microsoft.Extensions.Caching.Memory
{
    internal static class EntryLinkHelpers
    {
#if DOTNET5_4 || NETCORE50
        private static readonly AsyncLocal<EntryLink> _contextLink = new AsyncLocal<EntryLink>();

        public static EntryLink ContextLink
        {
            get { return _contextLink.Value; }
            set { _contextLink.Value = value; }
        }
#else
        private const string ContextLinkDataName = "EntryLinkHelpers.ContextLink";

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
