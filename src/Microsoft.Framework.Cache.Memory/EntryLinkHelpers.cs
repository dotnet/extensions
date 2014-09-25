// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if ASPNET50
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace Microsoft.Framework.Cache.Memory
{
    public static class EntryLinkHelpers
    {
#if ASPNET50
        private const string ContextLinkDataName = "klr.host.EntryLinkHelpers.ContextLink";

        public static IEntryLink ContextLink
        {
            get
            {
                var handle = CallContext.LogicalGetData(ContextLinkDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as IEntryLink;
            }
            set
            {
                CallContext.LogicalSetData(ContextLinkDataName, new ObjectHandle(value));
            }
        }
#else
        private static readonly AsyncLocal<IEntryLink> _contextLink = new AsyncLocal<IEntryLink>();

        public static IEntryLink ContextLink
        {
            get { return _contextLink.Value; }
            set { _contextLink.Value = value; }
        }
#endif
        public static IDisposable FlowContext(this IEntryLink link)
        {
            var priorLink = ContextLink;
            ContextLink = link;
            return new LinkContextReverter(priorLink);
        }

        private class LinkContextReverter : IDisposable
        {
            private readonly IEntryLink _priorLink;
            private bool _disposed;

            public LinkContextReverter(IEntryLink priorLink)
            {
                _priorLink = priorLink;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    ContextLink = _priorLink;
                }
            }
        }
    }
}