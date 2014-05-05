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
using System.Collections.Generic;

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

            if (serviceType == typeof(int))
            {
                return _timesGetServiceHasBeenInvoked;
            }
            else if (serviceType == typeof(string))
            {
                return _prefix + "FakeFallbackServiceProvider";
            }
            else if (serviceType == typeof(IEnumerable<string>))
            {
                return new[] { _prefix + "FakeFallbackServiceProvider" };
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
                throw new Exception();
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
