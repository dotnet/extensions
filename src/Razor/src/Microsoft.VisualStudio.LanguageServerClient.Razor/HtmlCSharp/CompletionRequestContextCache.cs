// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(CompletionRequestContextCache))]
    internal sealed class CompletionRequestContextCache
    {
        // Internal for testing
        internal static readonly int MaxCacheSize = 3;

        private readonly object _accessLock;
        private readonly List<CompletionRequestCacheItem> _completionRequests;
        private long _nextResultId;

        public CompletionRequestContextCache()
        {
            _accessLock = new object();
            _completionRequests = new List<CompletionRequestCacheItem>();
        }

        public long Set(CompletionRequestContext requestContext)
        {
            if (requestContext is null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            lock (_accessLock)
            {
                // If cache exceeds maximum size, remove the oldest list in the cache
                if (_completionRequests.Count >= MaxCacheSize)
                {
                    _completionRequests.RemoveAt(0);
                }

                var resultId = _nextResultId++;

                var cacheItem = new CompletionRequestCacheItem(resultId, requestContext);
                _completionRequests.Add(cacheItem);

                // Return generated resultId so completion list can later be retrieved from cache
                return resultId;
            }
        }

        public bool TryGet(long resultId, out CompletionRequestContext? requestContext)
        {
            lock (_accessLock)
            {
                // Search back -> front because the items in the back are the most recently added which are most frequently accessed.
                for (var i = _completionRequests.Count - 1; i >= 0; i--)
                {
                    var cacheItem = _completionRequests[i];
                    if (cacheItem.ResultId == resultId)
                    {
                        requestContext = cacheItem.RequestContext;
                        return true;
                    }
                }

                // A completion list associated with the given resultId was not found
                requestContext = null;
                return false;
            }
        }

        private record CompletionRequestCacheItem(long ResultId, CompletionRequestContext RequestContext);
    }
}
