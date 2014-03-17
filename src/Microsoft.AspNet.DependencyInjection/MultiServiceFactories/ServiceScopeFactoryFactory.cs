using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
{
    internal class ServiceScopeFactoryFactory : IMultiServiceFactory
    {
        private readonly ServiceProvider _provider;

        public ServiceScopeFactoryFactory(ServiceProvider provider)
        {
            _provider = provider;
        }

        public IMultiServiceFactory Scope(ServiceProvider scopedProvider)
        {
            return new ServiceScopeFactoryFactory(scopedProvider);
        }

        public object GetSingleService()
        {
            return new ServiceScopeFactory(_provider);
        }

        public IList GetMultiService()
        {
            return new List<IServiceScopeFactory> { new ServiceScopeFactory(_provider) };
        }
    }
}
