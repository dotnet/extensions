// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
{
    public interface IFakeFallbackServiceProvider : IServiceProvider, IDisposable
    {
    }

    public class FakeFallbackServiceProvider : IFakeFallbackServiceProvider
    {
        private int _timesGetServiceHasBeenInvoked = 0;
        private string _prefix;

        public FakeFallbackServiceProvider()
            : this(prefix: "")
        {
        }

        public FakeFallbackServiceProvider(string prefix)
        {
            _prefix = prefix;
        }

        public object GetService(Type serviceType)
        {
            _timesGetServiceHasBeenInvoked++;

            var typeInfo = serviceType.GetTypeInfo();
            if (typeInfo.IsGenericType &&
                typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var innerType = typeInfo.GenericTypeArguments.Single();
                var singleService = GetService(innerType);

                if (singleService == null)
                {
                    return Array.CreateInstance(innerType, 0);
                }
                else
                {
                    var serviceArray = Array.CreateInstance(innerType, 1);
                    serviceArray.SetValue(singleService, 0);
                    return serviceArray;
                }
            }
            else if (serviceType == typeof(int))
            {
                return _timesGetServiceHasBeenInvoked;
            }
            else if (serviceType == typeof(string))
            {
                return _prefix + "FakeFallbackServiceProvider";
            }
            else if (serviceType == typeof(IFakeFallbackService))
            {
                return new FakeService()
                {
                    Message = _prefix + "FakeFallbackServiceProvider"
                };
            }
            else if (serviceType == typeof(IFakeFallbackServiceProvider))
            {
                return this;
            }
            else if (serviceType == typeof(IServiceScopeFactory))
            {
                return new FakeFallbackScopeFactory(_prefix);
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            _prefix = "disposed-";
        }

        private class FakeFallbackScopeFactory : IServiceScopeFactory
        {
            private readonly string _prefix;

            public FakeFallbackScopeFactory(string prefix)
            {
                _prefix = prefix;
            }

            public IServiceScope CreateScope()
            {
                return new FakeFallbackScope(_prefix);
            }

            private class FakeFallbackScope : IServiceScope
            {
                private readonly IFakeFallbackServiceProvider _serviceProvider;

                public FakeFallbackScope(string prefix)
                {
                    _serviceProvider = new FakeFallbackServiceProvider("scope-" + prefix);
                }

                public IServiceProvider ServiceProvider
                {
                    get { return _serviceProvider; }
                }

                public void Dispose()
                {
                    _serviceProvider.Dispose();
                }
            }
        }
    }
}
