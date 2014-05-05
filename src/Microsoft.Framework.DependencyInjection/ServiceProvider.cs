// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Framework.DependencyInjection.ServiceLookup;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    internal class ServiceProvider : IServiceProvider, IDisposable
    {
        private readonly object _sync = new object();

        private readonly ServiceProvider _parent;
        private readonly ServiceTable _table;
        private readonly IServiceProvider _fallback;

        private readonly Dictionary<IService, object> _resolvedServices = new Dictionary<IService,object>();
        private ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        public ServiceProvider(IEnumerable<IServiceDescriptor> serviceDescriptors)
            : this(serviceDescriptors, fallbackServiceProvider: null)
        {
        }

        public ServiceProvider(
                IEnumerable<IServiceDescriptor> serviceDescriptors,
                IServiceProvider fallbackServiceProvider)
        {
            _table = new ServiceTable(serviceDescriptors);
            _fallback = fallbackServiceProvider;

            _table.Add(typeof(IServiceProvider), new ServiceProviderService());
            _table.Add(typeof(IServiceScopeFactory), new ServiceScopeService());
            _table.Add(typeof(IEnumerable<>), new OpenIEnumerableService(_table));
        }

        // This constructor is called exclusively to create a child scope from the parent
        internal ServiceProvider(ServiceProvider parent)
        {
            _parent = parent;
            _table = parent._table;
            _fallback = parent._fallback;

            // Rescope the fallback service provider if it contains an IServiceScopeFactory
            var scopeFactory = GetFallbackServiceOrNull<IServiceScopeFactory>();
            if (scopeFactory != null)
            {
                var scope = scopeFactory.CreateScope();
                _fallback = scope.ServiceProvider;
                _disposables.Add(scope);
            }
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            ServiceEntry entry;
            return _table.TryGetEntry(serviceType, out entry) ?
                ResolveService(entry.Last) :
                GetFallbackService(serviceType);
        }

        internal object ResolveService(IService service)
        {
            if (service.Lifecycle == LifecycleKind.Singleton && _parent != null)
            {
                return _parent.ResolveService(service);
            }
            if (service.Lifecycle == LifecycleKind.Transient)
            {
                return CaptureDisposable(service.Create(this));
            }
            else
            {
                // At this point we are either resolving a scoped service or a singleton
                // from the root ServiceProvider.
                lock (_sync)
                {
                    object resolved;
                    if (!_resolvedServices.TryGetValue(service, out resolved))
                    {
                        resolved = CaptureDisposable(service.Create(this));
                        _resolvedServices[service] = resolved;
                    }
                    return resolved;
                }
            }
        }

        public void Dispose()
        {
            var disposables = Interlocked.Exchange(ref _disposables, null);

            if (disposables != null)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        private object GetFallbackService(Type serviceType)
        {
            return _fallback != null ? _fallback.GetService(serviceType) : null;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        private T GetFallbackServiceOrNull<T>() where T : class
        {
            try
            {
                return (T)GetFallbackService(typeof(T));
            }
            catch
            {
                return null;
            }
        }

        private object CaptureDisposable(object service)
        {
            var disposable = service as IDisposable;
            if (disposable != null)
            {
                _disposables.Add(disposable);
            }
            return service;
        }
    }
}
