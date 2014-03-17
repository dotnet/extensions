namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
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
