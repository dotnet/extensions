// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    private static readonly Task<long> _zeroTimestamp = Task.FromResult<long>(0L);

    private readonly ConcurrentDictionary<string, Task<long>> _tagInvalidationTimes = [];

    private Task<long> _globalInvalidateTimestamp;

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return default; // nothing sensible to do
        }

        var now = CurrentTimestamp();
        InvalidateTagLocalCore(tag, now, isNow: true); // isNow to be 100% explicit
        return InvalidateL2TagAsync(tag, now, token);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Completion-checked")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Completion-checked")]
    public bool IsValid(CacheItem cacheItem)
    {
        var timestamp = cacheItem.CreationTimestamp;

        if (_globalInvalidateTimestamp.IsCompleted)
        {
            if (timestamp <= _globalInvalidateTimestamp.Result)
            {
                return false; // invalidated by wildcard
            }
        }

        var tags = cacheItem.Tags;
        switch (tags.Count)
        {
            case 0:
                return true;

            case 1:
                return !IsTagExpired(tags.GetSinglePrechecked(), timestamp);

            default:
                bool allValid = true;
                foreach (var tag in tags.GetSpanPrechecked())
                {
                    if (IsTagExpired(tag, timestamp))
                    {
                        allValid = false; // but check them all, to kick-off tag fetch
                    }
                }

                return allValid;
        }
    }

    internal long CurrentTimestamp() => _clock.GetUtcNow().UtcTicks;

    internal void PrefetchTags(TagSet tags)
    {
        if (HasBackendCache && !tags.IsEmpty)
        {
            // only needed if L2 exists
            switch (tags.Count)
            {
                case 1:
                    PrefetchTagWithBackendCache(tags.GetSinglePrechecked());
                    break;
                default:
                    foreach (var tag in tags.GetSpanPrechecked())
                    {
                        PrefetchTagWithBackendCache(tag);
                    }

                    break;
            }
        }
    }

    private void PrefetchTagWithBackendCache(string tag)
    {
        if (!_tagInvalidationTimes.TryGetValue(tag, out var pending))
        {
            _ = _tagInvalidationTimes.TryAdd(tag, SafeReadTagInvalidationAsync(tag));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Completion-checked")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Completion-checked")]
    private bool IsTagExpired(string tag, long timestamp)
    {
        if (!_tagInvalidationTimes.TryGetValue(tag, out var pending))
        {
            // not in the tag invalidation cache; if we have L2, need to check there
            if (HasBackendCache)
            {
                pending = SafeReadTagInvalidationAsync(tag);
                _ = _tagInvalidationTimes.TryAdd(tag, pending);
            }
            else
            {
                // not invalidated, and no L2 to check
                return false;
            }
        }

        if (pending.IsCompleted)
        {
            return timestamp > pending.Result;
        }
        else
        {
            return true; // assume invalid until completed
        }
    }

    private void InvalidateTagLocalCore(string tag, long timestamp, bool isNow)
    {
        var timestampTask = Task.FromResult<long>(timestamp);
        if (tag == TagSet.WildcardTag)
        {
            _globalInvalidateTimestamp = timestampTask;
            if (isNow && !HasBackendCache)
            {
                // no L2, so we don't need any prior invalidated tags any more; can clear
                _tagInvalidationTimes.Clear();
            }
        }
        else
        {
            _tagInvalidationTimes[tag] = timestampTask;
        }
    }
}
