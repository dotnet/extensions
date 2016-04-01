// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace Microsoft.Extensions.Caching.Memory.VSRC1
{
    internal static class EntryLinkHelpers
    {
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
