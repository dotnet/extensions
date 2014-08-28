// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.MemoryCache.Infrastructure;

namespace Microsoft.AspNet.MemoryCache
{
    public class MemoryCache : IMemoryCache
    {
        private readonly IDictionary<string, CacheEntry> _entries;
        private readonly ReaderWriterLockSlim _entryLock;
        private bool _disposed = false;

        private readonly Action<CacheEntry> _entryExpirationNotification;
        private readonly ISystemClock _clock;

        public MemoryCache()
            : this(new SystemClock())
        {
        }

        public MemoryCache(ISystemClock clock)
        {
            _entries = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
            _entryLock = new ReaderWriterLockSlim();
            _entryExpirationNotification = EntryExpired;
            _clock = clock;
            // TODO: Set up memory preassure notification
            // TODO: Set up expiration management
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

        private void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficency consider processing these expirations in batches.
            RemoveEntry(entry);
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