// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceScopeFactory : IServiceScopeFactory
    {
        private readonly ServiceProvider _provider;

        public ServiceScopeFactory(ServiceProvider provider)
        {
            _provider = provider;
        }

        public IServiceScope CreateScope()
        {
            return new ServiceScope(new ServiceProvider(_provider));
        }
    }
}
