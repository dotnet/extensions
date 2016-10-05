// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// An implementation of <see cref="IMemoryCache"/> using a dictionary to
    /// store its entries.
    /// </summary>
    public class MemoryCache : IMemoryCache
    {
        private readonly ConcurrentDictionary<object, CacheEntry> _entries;
        private bool _disposed;

        // We store the delegates locally to prevent allocations
        // every time a new CacheEntry is created.
        private readonly Action<CacheEntry> _setEntry;
        private readonly Action<CacheEntry> _entryExpirationNotification;

        private readonly ISystemClock _clock;

        private TimeSpan _expirationScanFrequency;
        private DateTimeOffset _lastExpirationScan;

        /// <summary>
        /// Creates a new <see cref="MemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            var options = optionsAccessor.Value;
            _entries = new ConcurrentDictionary<object, CacheEntry>();
            _setEntry = SetEntry;
            _entryExpirationNotification = EntryExpired;

            _clock = options.Clock ?? new SystemClock();
            if (options.CompactOnMemoryPressure)
            {
                GcNotification.Register(DoMemoryPreassureCollection, state: null);
            }
            _expirationScanFrequency = options.ExpirationScanFrequency;
            _lastExpirationScan = _clock.UtcNow;
        }

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        /// <inheritdoc />
        public ICacheEntry CreateEntry(object key)
        {
            CheckDisposed();

            return new CacheEntry(
                key,
                _setEntry,
                _entryExpirationNotification
            );
        }

        private void SetEntry(CacheEntry entry)
        {
            var utcNow = _clock.UtcNow;

            DateTimeOffset? absoluteExpiration = null;
            if (entry._absoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow + entry._absoluteExpirationRelativeToNow;
            }
            else if (entry._absoluteExpiration.HasValue)
            {
                absoluteExpiration = entry._absoluteExpiration;
            }

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (absoluteExpiration.HasValue)
            {
                if (!entry._absoluteExpiration.HasValue || absoluteExpiration.Value < entry._absoluteExpiration.Value)
                {
                    entry._absoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            var added = false;
            CacheEntry priorEntry;

            if (_entries.TryRemove(entry.Key, out priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            if (!entry.CheckExpired(utcNow))
            {
                if (_entries.TryAdd(entry.Key, entry))
                {
                    entry.AttachTokens();
                    added = true;
                }
            }

            if (priorEntry != null)
            {
                priorEntry.InvokeEvictionCallbacks();
            }

            if (!added)
            {
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItems();
        }

        /// <inheritdoc />
        public bool TryGetValue(object key, out object result)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var utcNow = _clock.UtcNow;
            result = null;
            bool found = false;
            CacheEntry expiredEntry = null;
            CheckDisposed();
            CacheEntry entry;
            if (_entries.TryGetValue(key, out entry))
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                if (entry.CheckExpired(utcNow))
                {
                    expiredEntry = entry;
                }
                else
                {
                    found = true;
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    // When this entry is retrieved in the scope of creating another entry,
                    // that entry needs a copy of these expiration tokens.
                    entry.PropagateOptions(CacheEntryHelper.Current);
                }
            }

            if (expiredEntry != null)
            {
                // TODO: For efficiency queue this up for batch removal
                RemoveEntry(expiredEntry);
            }

            StartScanForExpiredItems();

            return found;
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            CheckDisposed();
            CacheEntry entry;
            if (_entries.TryGetValue(key, out entry))
            {
                entry.SetExpired(EvictionReason.Removed);
            }

            if (entry != null)
            {
                // TODO: For efficiency consider processing these removals in batches.
                RemoveEntry(entry);
            }

            StartScanForExpiredItems();
        }

        private void RemoveEntry(CacheEntry entry)
        {
            // Only remove it if someone hasn't modified it since our lookup
            CacheEntry currentEntry;
            if (_entries.TryGetValue(entry.Key, out currentEntry)
                && object.ReferenceEquals(currentEntry, entry))
            {
                _entries.TryRemove(entry.Key, out currentEntry);
            }
            entry.InvokeEvictionCallbacks();
        }

        private void RemoveEntries(List<CacheEntry> entries)
        {
            foreach (var entry in entries)
            {
                // Only remove it if someone hasn't modified it since our lookup
                CacheEntry currentEntry;
                if (_entries.TryGetValue(entry.Key, out currentEntry)
                    && object.ReferenceEquals(currentEntry, entry))
                {
                    _entries.TryRemove(entry.Key, out currentEntry);
                }
            }

            foreach (var entry in entries)
            {
                entry.InvokeEvictionCallbacks();
            }
        }

        private void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            RemoveEntry(entry);
            StartScanForExpiredItems();
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        private void StartScanForExpiredItems()
        {
            var now = _clock.UtcNow;
            if (_expirationScanFrequency < now - _lastExpirationScan)
            {
                _lastExpirationScan = now;
                Task.Factory.StartNew(state => ScanForExpiredItems((MemoryCache)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void ScanForExpiredItems(MemoryCache cache)
        {
            List<CacheEntry> expiredEntries = new List<CacheEntry>();

            var now = cache._clock.UtcNow;
            foreach (var entry in cache._entries)
            {
                if (entry.Value.CheckExpired(now))
                {
                    expiredEntries.Add(entry.Value);
                }
            }

            cache.RemoveEntries(expiredEntries);
        }

        /// This is called after a Gen2 garbage collection. We assume this means there was memory pressure.
        /// Remove at least 10% of the total entries (or estimated memory?).
        private bool DoMemoryPreassureCollection(object state)
        {
            if (_disposed)
            {
                return false;
            }

            Compact(0.10);

            return true;
        }

        /// Remove at least the given percentage (0.10 for 10%) of the total entries (or estimated memory?), according to the following policy:
        /// 1. Remove all expired items.
        /// 2. Bucket by CacheItemPriority.
        /// ?. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public void Compact(double percentage)
        {
            List<CacheEntry> expiredEntries = new List<CacheEntry>();
            List<CacheEntry> lowPriEntries = new List<CacheEntry>();
            List<CacheEntry> normalPriEntries = new List<CacheEntry>();
            List<CacheEntry> highPriEntries = new List<CacheEntry>();
            List<CacheEntry> neverRemovePriEntries = new List<CacheEntry>();

            // Sort items by expired & priority status
            var now = _clock.UtcNow;
            foreach (var entry in _entries)
            {
                if (entry.Value.CheckExpired(now))
                {
                    expiredEntries.Add(entry.Value);
                }
                else
                {
                    switch (entry.Value.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add(entry.Value);
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(entry.Value);
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(entry.Value);
                            break;
                        case CacheItemPriority.NeverRemove:
                            neverRemovePriEntries.Add(entry.Value);
                            break;
                        default:
                            System.Diagnostics.Debug.Assert(false, "Not implemented: " + entry.Value.Priority);
                            break;
                    }
                }
            }

            int totalEntries = expiredEntries.Count + lowPriEntries.Count + normalPriEntries.Count + highPriEntries.Count + neverRemovePriEntries.Count;
            int removalCountTarget = (int)(totalEntries * percentage);

            ExpirePriorityBucket(removalCountTarget, expiredEntries, lowPriEntries);
            ExpirePriorityBucket(removalCountTarget, expiredEntries, normalPriEntries);
            ExpirePriorityBucket(removalCountTarget, expiredEntries, highPriEntries);

            RemoveEntries(expiredEntries);
        }

        /// Policy:
        /// ?. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        private void ExpirePriorityBucket(int removalCountTarget, List<CacheEntry> expiredEntries, List<CacheEntry> priorityEntries)
        {
            // Do we meet our quota by just removing expired entries?
            if (removalCountTarget <= expiredEntries.Count)
            {
                // No-op, we've met quota
                return;
            }
            if (expiredEntries.Count + priorityEntries.Count <= removalCountTarget)
            {
                // Expire all of the entries in this bucket
                foreach (var entry in priorityEntries)
                {
                    entry.SetExpired(EvictionReason.Capacity);
                }
                expiredEntries.AddRange(priorityEntries);
                return;
            }

            // Expire enough entries to reach our goal
            // TODO: Refine policy

            // LRU
            foreach (var entry in priorityEntries.OrderBy(entry => entry.LastAccessed))
            {
                entry.SetExpired(EvictionReason.Capacity);
                expiredEntries.Add(entry);
                if (removalCountTarget <= expiredEntries.Count)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                _disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(MemoryCache).FullName);
            }
        }
    }
}