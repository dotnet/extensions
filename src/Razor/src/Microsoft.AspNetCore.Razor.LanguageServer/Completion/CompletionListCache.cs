// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal sealed class CompletionListCache
    {
        // Internal for testing
        internal static readonly int MaxCacheSize = 3;

        private readonly object _accessLock;
        private readonly List<(long, IReadOnlyList<RazorCompletionItem>)> _resultIdToCompletionList;
        private long _nextResultId;

        public CompletionListCache()
        {
            _accessLock = new object();
            _resultIdToCompletionList = new List<(long, IReadOnlyList<RazorCompletionItem>)>();
        }

        public long Set(IReadOnlyList<RazorCompletionItem> razorCompletionList)
        {
            if (razorCompletionList is null)
            {
                throw new ArgumentNullException(nameof(razorCompletionList));
            }

            lock (_accessLock)
            {
                // If cache exceeds maximum size, remove the oldest list in the cache
                if (_resultIdToCompletionList.Count >= MaxCacheSize)
                {
                    _resultIdToCompletionList.RemoveAt(0);
                }

                var resultId = _nextResultId++;

                _resultIdToCompletionList.Add((resultId, razorCompletionList));

                // Return generated resultId so completion list can later be retrieved from cache
                return resultId;
            }
        }

        public bool TryGet(long resultId, out IReadOnlyList<RazorCompletionItem>? completionList)
        {
            lock (_accessLock)
            {
                // Search back -> front because the items in the back are the most recently added which are most frequently accessed.
                for (var i = _resultIdToCompletionList.Count - 1; i >= 0; i--)
                {
                    var (cachedResultId, cachedCompletionList) = _resultIdToCompletionList[i];
                    if (cachedResultId == resultId)
                    {
                        completionList = cachedCompletionList;
                        return true;
                    }
                }

                // A completion list associated with the given resultId was not found
                completionList = null;
                return false;
            }
        }
    }
}
