using System.Collections;

namespace Microsoft.AspNet.DependencyInjection.MultiServiceFactories
{
    internal interface IMultiServiceFactory
    {
        IMultiServiceFactory Scope(ServiceProvider scopedProvider);
        object GetSingleService();
        IList GetMultiService();
    }
}
