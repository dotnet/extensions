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
            PostEvictionCallbacks = options.PostEvictionCallbacks;
        }

        internal MemoryCacheEntryOptions Options { get; private set; }

        internal object Key { get; private set; }

        internal object Value { get; private set; }

        private bool IsExpired { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        internal IList<IDisposable> ExpirationTokenRegistrations { get; set; }

        internal IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; set; }

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

        // TODO: There's a possible race between AttachTokens and DetachTokens if a token fires almost immediately.
        // This may result in some registrations not getting disposed.
        internal void AttachTokens()
        {
            var expirationTokens = Options.ExpirationTokens;
            if (expirationTokens != null)
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

        private static void ExpirationTokensExpired(object obj)
        {
            var entry = (CacheEntry)obj;
            entry.SetExpired(EvictionReason.TokenExpired);
            entry._notifyCacheOfExpiration(entry);
        }

        // TODO: Thread safety
        private void DetachTokens()
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

        // TODO: Ensure a thread safe way to prevent these from being invoked more than once;
        internal void InvokeEvictionCallbacks()
        {
            if (PostEvictionCallbacks != null)
            {
                Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void InvokeCallbacks(CacheEntry entry)
        {
            var callbackRegistrations = entry.PostEvictionCallbacks;
            entry.PostEvictionCallbacks = null;

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