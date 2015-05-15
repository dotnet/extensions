// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Framework.Notification
{
    public class Notifier : INotifier
    {
        private readonly NotificationListenerCache _listeners = new NotificationListenerCache();
        
        private readonly INotifierMethodAdapter _methodAdapter;

        public Notifier(INotifierMethodAdapter methodAdapter)
        {
            _methodAdapter = methodAdapter;
        }

        public void EnlistTarget(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();

            var methodInfos = typeInfo.DeclaredMethods;

            foreach (var methodInfo in methodInfos)
            {
                var notificationNameAttribute = methodInfo.GetCustomAttribute<NotificationNameAttribute>();
                if (notificationNameAttribute != null)
                {
                    Enlist(notificationNameAttribute.Name, target, methodInfo);
                }
            }
        }

        private void Enlist(string notificationName, object target, MethodInfo methodInfo)
        {
            var entries = _listeners.GetOrAdd(
                notificationName,
                _ => new ConcurrentBag<ListenerEntry>());

            entries.Add(new ListenerEntry(target, methodInfo));
        }

        public bool ShouldNotify(string notificationName)
        {
            return _listeners.ContainsKey(notificationName);
        }

        public void Notify(string notificationName, object parameters)
        {
            if (parameters == null)
            {
                return;
            }

            ConcurrentBag<ListenerEntry> entries;
            if (_listeners.TryGetValue(notificationName, out entries))
            {
                foreach (var entry in entries)
                {
                    var succeeded = false;
                    foreach (var adapter in entry.Adapters)
                    {
                        if (adapter(entry.Target, parameters))
                        {
                            succeeded = true;
                            break;
                        }
                    }

                    if (!succeeded)
                    {
                        var newAdapter = _methodAdapter.Adapt(entry.MethodInfo, parameters.GetType());
                        succeeded = newAdapter(entry.Target, parameters);
                        Debug.Assert(succeeded);

                        entry.Adapters.Add(newAdapter);
                    }
                }
            }
        }

        private class NotificationListenerCache : ConcurrentDictionary<string, ConcurrentBag<ListenerEntry>>
        {
            public NotificationListenerCache()
                : base(StringComparer.Ordinal)
            {
            }
        }

        private class ListenerEntry
        {
            public ListenerEntry(object target, MethodInfo methodInfo)
            {
                Target = target;
                MethodInfo = methodInfo;

                Adapters = new ConcurrentBag<Func<object, object, bool>>();
            }

            public ConcurrentBag<Func<object, object, bool>> Adapters { get; }

            public MethodInfo MethodInfo { get; }

            public object Target { get; }
        }
    }
}
