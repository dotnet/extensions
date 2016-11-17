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

        private ICollection<KeyValuePair<object, CacheEntry>> EntriesCollection => _entries;

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
            if (_disposed)
            {
                // No-op instead of throwing since this is called during CacheEntry.Dispose
                return;
            }

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

            CacheEntry priorEntry;
            if (_entries.TryGetValue(entry.Key, out priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            if (!entry.CheckExpired(utcNow))
            {
                var entryAdded = false;

                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = _entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = _entries.TryUpdate(entry.Key, entry, priorEntry);

                    if (!entryAdded)
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = _entries.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                if (priorEntry != null)
                {
                    priorEntry.InvokeEvictionCallbacks();
                }
            }
            else
            {
                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    RemoveEntry(priorEntry);
                }
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

            CheckDisposed();

            result = null;
            var utcNow = _clock.UtcNow;
            var found = false;

            CacheEntry entry;
            if (_entries.TryGetValue(key, out entry))
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to a race with SetExpired during SetEntry.
                if (entry.CheckExpired(utcNow) && entry.EvictionReason != EvictionReason.Replaced)
                {
                    // TODO: For efficiency queue this up for batch removal
                    RemoveEntry(entry);
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
            if (_entries.TryRemove(key, out entry))
            {
                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItems();
        }

        private void RemoveEntry(CacheEntry entry)
        {
            if (EntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry)))
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
            var now = cache._clock.UtcNow;
            foreach (var entry in cache._entries.Values)
            {
                if (entry.CheckExpired(now))
                {
                    cache.RemoveEntry(entry);
                }
            }
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
        /// 3. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public void Compact(double percentage)
        {
            var entriesToRemove = new List<CacheEntry>();
            var lowPriEntries = new List<CacheEntry>();
            var normalPriEntries = new List<CacheEntry>();
            var highPriEntries = new List<CacheEntry>();

            // Sort items by expired & priority status
            var now = _clock.UtcNow;
            foreach (var entry in _entries.Values)
            {
                if (entry.CheckExpired(now))
                {
                    entriesToRemove.Add(entry);
                }
                else
                {
                    switch (entry.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.NeverRemove:
                            break;
                        default:
                            throw new NotSupportedException("Not implemented: " + entry.Priority);
                    }
                }
            }

            int removalCountTarget = (int)(_entries.Count * percentage);

            ExpirePriorityBucket(removalCountTarget, entriesToRemove, lowPriEntries);
            ExpirePriorityBucket(removalCountTarget, entriesToRemove, normalPriEntries);
            ExpirePriorityBucket(removalCountTarget, entriesToRemove, highPriEntries);

            foreach (var entry in entriesToRemove)
            {
                RemoveEntry(entry);
            }
        }

        /// Policy:
        /// 1. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        private void ExpirePriorityBucket(int removalCountTarget, List<CacheEntry> entriesToRemove, List<CacheEntry> priorityEntries)
        {
            // Do we meet our quota by just removing expired entries?
            if (removalCountTarget <= entriesToRemove.Count)
            {
                // No-op, we've met quota
                return;
            }
            if (entriesToRemove.Count + priorityEntries.Count <= removalCountTarget)
            {
                // Expire all of the entries in this bucket
                foreach (var entry in priorityEntries)
                {
                    entry.SetExpired(EvictionReason.Capacity);
                }
                entriesToRemove.AddRange(priorityEntries);
                return;
            }

            // Expire enough entries to reach our goal
            // TODO: Refine policy

            // LRU
            foreach (var entry in priorityEntries.OrderBy(entry => entry.LastAccessed))
            {
                entry.SetExpired(EvictionReason.Capacity);
                entriesToRemove.Add(entry);
                if (removalCountTarget <= entriesToRemove.Count)
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