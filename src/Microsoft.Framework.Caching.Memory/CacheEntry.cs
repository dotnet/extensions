// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
#if !NET45 && !DNX451 && !DNXCORE50
using System.Threading.Tasks;
#endif

namespace Microsoft.Framework.Caching.Memory
{
    internal class CacheEntry
    {
        private static readonly Action<object> ExpirationCallback = TriggerExpired;

        private readonly Action<CacheEntry> _notifyCacheOfExpiration;

        private readonly DateTimeOffset? _absoluteExpiration;

        internal CacheEntry(
            string key,
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

        internal string Key { get; private set; }

        internal object Value { get; private set; }

        private bool IsExpired { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        internal IList<IDisposable> TriggerRegistrations { get; set; }

        internal IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; set; }

        internal DateTimeOffset LastAccessed { get; set; }

        internal bool CheckExpired(DateTimeOffset now)
        {
            return IsExpired || CheckForExpiredTime(now) || CheckForExpiredTriggers();
        }

        internal void SetExpired(EvictionReason reason)
        {
            IsExpired = true;
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            DetachTriggers();
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

        internal bool CheckForExpiredTriggers()
        {
            var triggers = Options.Triggers;
            if (triggers != null)
            {
                for (int i = 0; i < triggers.Count; i++)
                {
                    var trigger = triggers[i];
                    if (trigger.IsExpired)
                    {
                        SetExpired(EvictionReason.Triggered);
                        return true;
                    }
                }
            }
            return false;
        }

        // TODO: There's a possible race between AttachTriggers and DetachTriggers if a trigger fires almost immediately.
        // This may result in some registrations not getting disposed.
        internal void AttachTriggers()
        {
            var triggers = Options.Triggers;
            if (triggers != null)
            {
                for (int i = 0; i < triggers.Count; i++)
                {
                    var trigger = triggers[i];
                    if (trigger.ActiveExpirationCallbacks)
                    {
                        if (TriggerRegistrations == null)
                        {
                            TriggerRegistrations = new List<IDisposable>(1);
                        }
                        var registration = trigger.RegisterExpirationCallback(ExpirationCallback, this);
                        TriggerRegistrations.Add(registration);
                    }
                }
            }
        }

        private static void TriggerExpired(object obj)
        {
            var entry = (CacheEntry)obj;
            entry.SetExpired(EvictionReason.Triggered);
            entry._notifyCacheOfExpiration(entry);
        }

        // TODO: Thread safety
        private void DetachTriggers()
        {
            var registrations = TriggerRegistrations;
            if (registrations != null)
            {
                TriggerRegistrations = null;
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
            var callbacks = PostEvictionCallbacks;
            PostEvictionCallbacks = null;
            if (callbacks != null)
            {
#if NET45 || DNX451 || DNXCORE50
                ThreadPool.QueueUserWorkItem(InvokeCallbacks, callbacks);
#else
                Task.Run(() => InvokeCallbacks(callbacks));
#endif
            }
        }

        private void InvokeCallbacks(object state)
        {
            var callbackRegistrations = (IList<PostEvictionCallbackRegistration>)state;
            for (int i = 0; i < callbackRegistrations.Count; i++)
            {
                var registration = callbackRegistrations[i];

                try
                {
                    if (registration.EvictionCallback != null)
                    {
                        registration.EvictionCallback(Key, Value, EvictionReason, registration.State);
                    }
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