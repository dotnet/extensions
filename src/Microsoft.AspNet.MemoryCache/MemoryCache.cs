// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.MemoryCache.Infrastructure;

namespace Microsoft.AspNet.MemoryCache
{
    public class MemoryCache : IMemoryCache
    {
        private readonly IDictionary<string, CacheEntry> _entries;
        private readonly ReaderWriterLockSlim _entryLock;
        private bool _disposed;

        private readonly Action<CacheEntry> _entryExpirationNotification;
        private readonly ISystemClock _clock;

        public MemoryCache()
            : this(new SystemClock(), listenForMemoryPressure: true)
        {
        }

        /// <summary>
        /// Creates a new MemoryCache instance. This overload is intended for testing purposes.
        /// </summary>
        /// <param name="clock"></param>
        /// <param name="listenForMemoryPressure"></param>
        public MemoryCache(ISystemClock clock, bool listenForMemoryPressure)
        {
            _entries = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
            _entryLock = new ReaderWriterLockSlim();
            _entryExpirationNotification = EntryExpired;
            _clock = clock;
            if (listenForMemoryPressure)
            {
                GcNotification.Register(DoMemoryPreassureCollection, state: null);
            }
            // TODO: Set up expiration monitoring
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

        public object Set(string key, object state, Func<ICacheAddContext, object> create)
        {
            CheckDisposed();
            CacheEntry priorEntry = null;
            var now = _clock.UtcNow;
            var context = new CacheAddContext(key) { State = state, CreationTime = now };
            object value = create(context);
            var entry = new CacheEntry(context, value, _entryExpirationNotification);
            bool added = false;

            _entryLock.EnterWriteLock();
            try
            {
                if (_entries.TryGetValue(key, out priorEntry))
                {
                    _entries.Remove(key);
                    priorEntry.SetExpired(EvictionReason.Replaced);
                }

                if (!entry.CheckExpired(now))
                {
                    _entries[key] = entry;
                    // TODO: Add to timer queue
                    entry.AttachTriggers();
                    added = true;
                }
            }
            finally
            {
                _entryLock.ExitWriteLock();
            }
            if (priorEntry != null)
            {
                // TODO: Execute on a threadpool thread.
                priorEntry.InvokeEvictionCallbacks();
            }
            if (!added)
            {
                // TODO: Execute on a threadpool thread.
                entry.InvokeEvictionCallbacks();
            }
            return value;
        }

        public bool TryGetValue(string key, out object value)
        {
            value = null;
            CacheEntry expiredEntry = null;
            bool found = false;
            CheckDisposed();
            _entryLock.EnterReadLock();
            try
            {
                CacheEntry entry;
                if (_entries.TryGetValue(key, out entry))
                {
                    // Check if expired due to triggers, timers, etc. and if so, remove it.
                    if (entry.CheckExpired(_clock.UtcNow))
                    {
                        expiredEntry = entry;
                    }
                    else
                    {
                        // Refresh sliding expiration, etc.
                        entry.LastAccessed = _clock.UtcNow;
                        value = entry.Value;
                        found = true;
                    }
                }
            }
            finally
            {
                _entryLock.ExitReadLock();
            }

            if (expiredEntry != null)
            {
                // TODO: For efficency queue this up for batch removal
                RemoveEntry(expiredEntry);
            }

            return found;
        }

        public void Remove(string key)
        {
            CheckDisposed();
            CacheEntry entry;
            _entryLock.EnterReadLock();
            try
            {
                if (_entries.TryGetValue(key, out entry))
                {
                    entry.SetExpired(EvictionReason.Removed);
                }
            }
            finally
            {
                _entryLock.ExitReadLock();
            }

            if (entry != null)
            {
                // TODO: For efficency consider processing these removals in batches.
                RemoveEntry(entry);
            }
        }

        private void RemoveEntry(CacheEntry entry)
        {
            _entryLock.EnterWriteLock();
            try
            {
                // Only remove it if someone hasn't modified it since our lookup
                CacheEntry currentEntry;
                if (_entries.TryGetValue(entry.Context.Key, out currentEntry)
                    && object.ReferenceEquals(currentEntry, entry))
                {
                    _entries.Remove(entry.Context.Key);
                }
            }
            finally
            {
                _entryLock.ExitWriteLock();
            }
            // TODO: Execute on a threadpool thread.
            entry.InvokeEvictionCallbacks();
        }

        private void RemoveEntries(IEnumerable<CacheEntry> entries)
        {
            _entryLock.EnterWriteLock();
            try
            {
                foreach (var entry in entries)
                {
                    // Only remove it if someone hasn't modified it since our lookup
                    CacheEntry currentEntry;
                    if (_entries.TryGetValue(entry.Context.Key, out currentEntry)
                        && object.ReferenceEquals(currentEntry, entry))
                    {
                        _entries.Remove(entry.Context.Key);
                    }
                }
            }
            finally
            {
                _entryLock.ExitWriteLock();
            }

            foreach (var entry in entries)
            {
                // TODO: Execute on a threadpool thread.
                entry.InvokeEvictionCallbacks();
            }
        }

        private void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficency consider processing these expirations in batches.
            RemoveEntry(entry);
        }

        /// This is called after a Gen2 garbage collection. We assume this means there was memory preassure.
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
        /// 2. Bucket by CachePreservationPriority.
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

            _entryLock.EnterReadLock();
            try
            {
                // Sort items by expired & priority status
                var now = _clock.UtcNow;
                foreach (var entry in _entries.Values)
                {
                    if (entry.CheckExpired(now))
                    {
                        expiredEntries.Add(entry);
                    }
                    else
                    {
                        switch (entry.Context.Priority)
                        {
                            case CachePreservationPriority.Low:
                                lowPriEntries.Add(entry);
                                break;
                            case CachePreservationPriority.Normal:
                                normalPriEntries.Add(entry);
                                break;
                            case CachePreservationPriority.High:
                                highPriEntries.Add(entry);
                                break;
                            case CachePreservationPriority.NeverRemove:
                                neverRemovePriEntries.Add(entry);
                                break;
                            default:
                                System.Diagnostics.Debug.Assert(false, "Not implemented: " + entry.Context.Priority);
                                break;
                        }
                    }
                }

                int totalEntries = expiredEntries.Count + lowPriEntries.Count + normalPriEntries.Count + highPriEntries.Count + neverRemovePriEntries.Count;
                int removalCountTarget = (int)(totalEntries * percentage);

                ExpirePriorityBucket(removalCountTarget, expiredEntries, lowPriEntries);
                ExpirePriorityBucket(removalCountTarget, expiredEntries, normalPriEntries);
                ExpirePriorityBucket(removalCountTarget, expiredEntries, highPriEntries);
            }
            finally
            {
                _entryLock.ExitReadLock();
            }

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