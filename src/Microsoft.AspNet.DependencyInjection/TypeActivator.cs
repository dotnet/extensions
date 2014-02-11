using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public class TypeActivator : ITypeActivator
    {
        private readonly IServiceProvider _services;

        public TypeActivator(IServiceProvider services)
        {
            _services = services;
        }

        public object CreateInstance(Type instanceType)
        {
            return ActivatorUtilities.CreateInstance(_services, instanceType);
        }
    }
}