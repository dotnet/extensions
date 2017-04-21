// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DiagnosticAdapter.Internal;

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public class DiagnosticSourceAdapter : IObserver<KeyValuePair<string, object>>
    {
        private readonly Listener _listener;
        private readonly IDiagnosticSourceMethodAdapter _methodAdapter;

        public DiagnosticSourceAdapter(object target)
            : this(target, (Func<string, bool>)null, new ProxyDiagnosticSourceMethodAdapter())
        {
        }

        public DiagnosticSourceAdapter(object target, Func<string, bool> isEnabled)
            : this(target, isEnabled, methodAdapter: new ProxyDiagnosticSourceMethodAdapter())
        {
        }

        public DiagnosticSourceAdapter(object target, Func<string, object, object, bool> isEnabled)
            : this(target, isEnabled, methodAdapter: new ProxyDiagnosticSourceMethodAdapter())
        {
        }

        public DiagnosticSourceAdapter(
            object target,
            Func<string, bool> isEnabled,
            IDiagnosticSourceMethodAdapter methodAdapter)
            : this(target, isEnabled: (isEnabled == null) ? (Func<string, object, object, bool>)null : (a, b, c) => isEnabled(a), methodAdapter: methodAdapter)
        {
        }

        public DiagnosticSourceAdapter(
            object target,
            Func<string, object, object, bool> isEnabled,
            IDiagnosticSourceMethodAdapter methodAdapter)
        {
            _methodAdapter = methodAdapter;
            _listener = EnlistTarget(target, isEnabled);
        }

        private static Listener EnlistTarget(object target, Func<string, object, object, bool> isEnabled)
        {
            var listener = new Listener(target, isEnabled);

            var typeInfo = target.GetType().GetTypeInfo();
            var methodInfos = typeInfo.DeclaredMethods;
            foreach (var methodInfo in methodInfos)
            {
                var diagnosticNameAttribute = methodInfo.GetCustomAttribute<DiagnosticNameAttribute>();
                if (diagnosticNameAttribute != null)
                {
                    var subscription = new Subscription(methodInfo);
                    listener.Subscriptions.Add(diagnosticNameAttribute.Name, subscription);
                }
            }

            return listener;
        }

        public bool IsEnabled(string diagnosticName)
        {
            return IsEnabled(diagnosticName, null);
        }

        public bool IsEnabled(string diagnosticName, object arg1, object arg2 = null)
        {
            if (_listener.Subscriptions.Count == 0)
            {
                return false;
            }

            return
                _listener.Subscriptions.ContainsKey(diagnosticName) &&
                (_listener.IsEnabled == null || _listener.IsEnabled(diagnosticName, arg1, arg2));
        }

        public void Write(string diagnosticName, object parameters)
        {
            if (parameters == null)
            {
                return;
            }

            Subscription subscription;
            if (!_listener.Subscriptions.TryGetValue(diagnosticName, out subscription))
            {
                return;
            }

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
                try
                {
                    succeeded = newAdapter(_listener.Target, parameters);
                }
                catch (InvalidProxyOperationException ex)
                {
                    throw new InvalidOperationException(
                        Resources.FormatConverter_UnableToGenerateProxy(subscription.MethodInfo.Name),
                        ex);
                }
                Debug.Assert(succeeded);

                subscription.Adapters.Add(newAdapter);
            }
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            Write(value.Key, value.Value);
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
            public Listener(object target, Func<string, object, object, bool> isEnabled)
            {
                Target = target;
                IsEnabled = isEnabled;

                Subscriptions = new Dictionary<string, Subscription>(StringComparer.Ordinal);
            }

            public Func<string, object, object, bool> IsEnabled { get; }

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
