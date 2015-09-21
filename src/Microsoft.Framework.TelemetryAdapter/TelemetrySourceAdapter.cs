// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class TelemetrySourceAdapter : IObserver<KeyValuePair<string, object>>
    {
        private readonly Listener _listener;
        private readonly ITelemetrySourceMethodAdapter _methodAdapter;

        public TelemetrySourceAdapter(object listener)
            : this(listener, new ProxyTelemetrySourceMethodAdapter())
        {
        }

        public TelemetrySourceAdapter(object target, ITelemetrySourceMethodAdapter methodAdapter)
        {
            _methodAdapter = methodAdapter;

            _listener = EnlistTarget(target);
        }

        private static Listener EnlistTarget(object target)
        {
            var listener = new Listener(target);

            var typeInfo = target.GetType().GetTypeInfo();
            var methodInfos = typeInfo.DeclaredMethods;
            foreach (var methodInfo in methodInfos)
            {
                var notificationNameAttribute = methodInfo.GetCustomAttribute<TelemetryNameAttribute>();
                if (notificationNameAttribute != null)
                {
                    var subscription = new Subscription(methodInfo);
                    listener.Subscriptions.Add(notificationNameAttribute.Name, subscription);
                }
            }

            return listener;
        }

        public bool IsEnabled(string telemetryName)
        {
            if (_listener.Subscriptions.Count == 0)
            {
                return false;
            }

            return _listener.Subscriptions.ContainsKey(telemetryName);
        }

        public void WriteTelemetry(string telemetryName, object parameters)
        {
            if (parameters == null)
            {
                return;
            }

            Subscription subscription;
            if (_listener.Subscriptions.TryGetValue(telemetryName, out subscription))
            {
                var succeeded = false;
                foreach (var adapter in subscription.Adapters)
                {
                    if (adapter(_listener.Target, parameters))
                    {
                        succeeded = true;
                        break;
                    }
                }

                if (!succeeded)
                {
                    var newAdapter = _methodAdapter.Adapt(subscription.MethodInfo, parameters.GetType());
                    succeeded = newAdapter(_listener.Target, parameters);
                    Debug.Assert(succeeded);

                    subscription.Adapters.Add(newAdapter);
                }
            }
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            WriteTelemetry(value.Key, value.Value);
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
            // Do nothing
        }

        void IObserver<KeyValuePair<string, object>>.OnCompleted()
        {
            // Do nothing
        }

        private class Listener
        {
            public Listener(object target)
            {
                Target = target;

                Subscriptions = new Dictionary<string, Subscription>(StringComparer.Ordinal);
            }

            public object Target { get; }

            public Dictionary<string, Subscription> Subscriptions { get; }
        }

        private class Subscription
        {
            public Subscription(MethodInfo methodInfo)
            {
                MethodInfo = methodInfo;

                Adapters = new ConcurrentBag<Func<object, object, bool>>();
            }

            public ConcurrentBag<Func<object, object, bool>> Adapters { get; }

            public MethodInfo MethodInfo { get; }
        }
    }
}
