// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<string, long> _tagInvalidationTimes = [];

    private long _globalInvalidateTimestamp;

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken token = default)
    {
        InvalidateTagCore(tag);
        return default;
    }

    public bool IsValid(CacheItem cacheItem)
    {
        long globalInvalidationTimestamp;
        if (IntPtr.Size < sizeof(long))
        {
            // prevent torn values on x86
            globalInvalidationTimestamp = Interlocked.Read(ref _globalInvalidateTimestamp);
        }
        else
        {
            globalInvalidationTimestamp = _globalInvalidateTimestamp;
        }

        var timestamp = cacheItem.CreationTimestamp;
        if (timestamp <= globalInvalidationTimestamp)
        {
            return false; // invalidated by wildcard
        }

        var tags = cacheItem.Tags;
        switch (tags.Count)
        {
            case 0:
                return true;
            case 1:
                return !(_tagInvalidationTimes.TryGetValue(tags.GetSinglePrechecked(), out var tagInvalidatedTimestamp) && timestamp <= tagInvalidatedTimestamp);
            default:
                foreach (var tag in tags.GetSpanPrechecked())
                {
                    if (_tagInvalidationTimes.TryGetValue(tag, out tagInvalidatedTimestamp) && timestamp <= tagInvalidatedTimestamp)
                    {
                        return false;
                    }
                }

                return true;
        }
    }

    internal long CurrentTimestamp() => _clock.GetUtcNow().UtcTicks;

    private void InvalidateTagCore(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            // nothing sensible to do
            return;
        }

        var now = CurrentTimestamp();
        if (tag == TagSet.WildcardTag)
        {
            // on modern runtimes JIT will do a good job of dead-branch removal for this
            if (IntPtr.Size < sizeof(long))
            {
                // prevent torn values on x86
                _ = Interlocked.Exchange(ref _globalInvalidateTimestamp, now);
            }
            else
            {
                _globalInvalidateTimestamp = now;
            }
        }
        else
        {
            _tagInvalidationTimes[tag] = now;
        }
    }
}
