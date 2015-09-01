// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class DefaultTelemetrySourceAdapter : TelemetrySourceAdapter
    {
        private readonly ListenerCache _listeners = new ListenerCache();
        
        private readonly ITelemetrySourceMethodAdapter _methodAdapter;

        public DefaultTelemetrySourceAdapter(ITelemetrySourceMethodAdapter methodAdapter)
        {
            _methodAdapter = methodAdapter;
        }

        public override void EnlistTarget(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();

            var methodInfos = typeInfo.DeclaredMethods;

            foreach (var methodInfo in methodInfos)
            {
                var notificationNameAttribute = methodInfo.GetCustomAttribute<TelemetryNameAttribute>();
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

        public override bool IsEnabled(string telemetryName)
        {
            if (_listeners.Count == 0)
            {
                return false;
            }

            return _listeners.ContainsKey(telemetryName);
        }

        public override void WriteTelemetry(string telemetryName, object parameters)
        {
            if (parameters == null)
            {
                return;
            }

            ConcurrentBag<ListenerEntry> entries;
            if (_listeners.TryGetValue(telemetryName, out entries))
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

        private class ListenerCache : ConcurrentDictionary<string, ConcurrentBag<ListenerEntry>>
        {
            public ListenerCache()
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
