// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.MemoryCache
{
    internal class CacheEntry
    {
        private static readonly Action<object> ExpirationCallback = TriggerExpired;

        private Action<CacheEntry> _notifyCacheOfExpiration;
        private bool _isExpired;
        
        internal CacheEntry(CacheAddContext context, object value, Action<CacheEntry> notifyCacheOfExpiration)
        {
            Context = context;
            Value = value;
            _notifyCacheOfExpiration = notifyCacheOfExpiration;
        }

        internal CacheAddContext Context { get; private set; }

        internal bool IsExpired
        {
            get
            {
                return _isExpired || CheckForExpiredTriggers();
            }
            set
            {
                _isExpired = value;
            }
        }

        internal EvictionReason EvictionReason { get; set; }

        internal object Value { get; private set; }

        internal IList<IDisposable> TriggerRegistrations { get; set; }

        internal bool CheckForExpiredTriggers()
        {
            var triggers = Context.Triggers;
            if (triggers != null)
            {
                for (int i = 0; i < triggers.Count; i++)
                {
                    var trigger = triggers[i];
                    if (trigger.IsExpired)
                    {
                        IsExpired = true;
                        if (EvictionReason == EvictionReason.None)
                        {
                            EvictionReason = EvictionReason.Triggered;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        // TODO: There's a possible race between AttachTriggers and DetachTriggers if a trigger fires almost immidiately.
        // This may result in some registrations not getting disposed.
        internal void AttachTriggers()
        {
            var triggers = Context.Triggers;
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
            entry.IsExpired = true;
            if (entry.EvictionReason == EvictionReason.None)
            {
                entry.EvictionReason = EvictionReason.Triggered;
            }
            entry._notifyCacheOfExpiration(entry);
        }

        internal void DetatchTriggers()
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
    }
}