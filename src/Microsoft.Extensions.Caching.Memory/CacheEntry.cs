// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntry
    {
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;

        private readonly Action<CacheEntry> _notifyCacheOfExpiration;

        private readonly DateTimeOffset? _absoluteExpiration;

        private IList<PostEvictionCallbackRegistration> _postEvictionCallbacks;

        internal readonly object _lock = new object();

        internal CacheEntry(
            object key,
            object value,
            DateTimeOffset utcNow,
            DateTimeOffset? absoluteExpiration,
            MemoryCacheEntryOptions options,
            Action<CacheEntry> notifyCacheOfExpiration)
        {
            Key = key;
            Value = value;
            LastAccessed = utcNow;
            Options = options;
            _notifyCacheOfExpiration = notifyCacheOfExpiration;
            _absoluteExpiration = absoluteExpiration;
            _postEvictionCallbacks = options.PostEvictionCallbacks;
        }

        internal MemoryCacheEntryOptions Options { get; private set; }

        internal object Key { get; private set; }

        internal object Value { get; private set; }

        private bool IsExpired { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        internal IList<IDisposable> ExpirationTokenRegistrations { get; set; }

        internal DateTimeOffset LastAccessed { get; set; }

        internal bool CheckExpired(DateTimeOffset now)
        {
            return IsExpired || CheckForExpiredTime(now) || CheckForExpiredTokens();
        }

        internal void SetExpired(EvictionReason reason)
        {
            IsExpired = true;
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            DetachTokens();
        }

        private bool CheckForExpiredTime(DateTimeOffset now)
        {
            if (_absoluteExpiration.HasValue && _absoluteExpiration.Value <= now)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            if (Options.SlidingExpiration.HasValue
                && (now - LastAccessed) >= Options.SlidingExpiration)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            return false;
        }

        internal bool CheckForExpiredTokens()
        {
            var expiredTokens = Options.ExpirationTokens;
            if (expiredTokens != null)
            {
                for (int i = 0; i < expiredTokens.Count; i++)
                {
                    var expiredToken = expiredTokens[i];
                    if (expiredToken.HasChanged)
                    {
                        SetExpired(EvictionReason.TokenExpired);
                        return true;
                    }
                }
            }
            return false;
        }

        internal void AttachTokens()
        {
            var expirationTokens = Options.ExpirationTokens;
            if (expirationTokens != null)
            {
                lock (_lock)
                {
                    for (int i = 0; i < expirationTokens.Count; i++)
                    {
                        var expirationToken = expirationTokens[i];
                        if (expirationToken.ActiveChangeCallbacks)
                        {
                            if (ExpirationTokenRegistrations == null)
                            {
                                ExpirationTokenRegistrations = new List<IDisposable>(1);
                            }
                            var registration = expirationToken.RegisterChangeCallback(ExpirationCallback, this);
                            ExpirationTokenRegistrations.Add(registration);
                        }
                    }
                }
            }
        }

        private static void ExpirationTokensExpired(object obj)
        {
            // start a new thread to avoid issues with callbacks called from RegisterChangeCallback
            Task.Factory.StartNew(state =>
            {
                var entry = (CacheEntry)state;
                entry.SetExpired(EvictionReason.TokenExpired);
                entry._notifyCacheOfExpiration(entry);
            }, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void DetachTokens()
        {
            lock(_lock)
            {
                var registrations = ExpirationTokenRegistrations;
                if (registrations != null)
                {
                    ExpirationTokenRegistrations = null;
                    for (int i = 0; i < registrations.Count; i++)
                    {
                        var registration = registrations[i];
                        registration.Dispose();
                    }
                }
            }
        }

        internal void InvokeEvictionCallbacks()
        {
            if (_postEvictionCallbacks != null)
            {
                Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void InvokeCallbacks(CacheEntry entry)
        {
            var callbackRegistrations = Interlocked.Exchange(ref entry._postEvictionCallbacks, null);

            if (callbackRegistrations == null)
            {
                return;
            }

            for (int i = 0; i < callbackRegistrations.Count; i++)
            {
                var registration = callbackRegistrations[i];

                try
                {
                    registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
                }
                catch (Exception)
                {
                    // This will be invoked on a background thread, don't let it throw.
                    // TODO: LOG
                }
            }
        }
    }
}