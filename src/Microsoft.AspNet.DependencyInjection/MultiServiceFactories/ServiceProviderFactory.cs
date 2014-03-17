using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
{
    internal class ServiceProviderFactory : IMultiServiceFactory
    {
        private readonly ServiceProvider _provider;

        public ServiceProviderFactory(ServiceProvider provider)
        {
            _provider = provider;
        }

        public IMultiServiceFactory Scope(ServiceProvider scopedProvider)
        {
            return new ServiceProviderFactory(scopedProvider);
        }

        public object GetSingleService()
        {
            return _provider;
        }

        public IList GetMultiService()
        {
            return new List<IServiceProvider> { _provider };
        }
    }
}
