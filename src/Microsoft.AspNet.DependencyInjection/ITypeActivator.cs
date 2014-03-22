using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface ITypeActivator
    {
        object CreateInstance(IServiceProvider services, Type instanceType, params object[] parameters);
    }
}
