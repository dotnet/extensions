using System;

namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
{
    internal class ServiceScope : IServiceScope
    {
        private readonly ServiceProvider _scopedProvider;

        public ServiceScope(ServiceProvider scopedProvider)
        {
            _scopedProvider = scopedProvider;
        }

        public IServiceProvider ServiceProvider
        {
            get { return _scopedProvider.GetService<IServiceProvider>(); }
        }

        public void Dispose()
        {
            _scopedProvider.Dispose();
        }
    }
}
