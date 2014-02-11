using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface ITypeActivator
    {
        object CreateInstance(Type instanceType);
    }
}