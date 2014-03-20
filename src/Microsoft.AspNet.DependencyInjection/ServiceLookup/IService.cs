namespace Microsoft.AspNet.DependencyInjection.ServiceLookup
{
    internal interface IService
    {
        IService Next { get; set; }

        LifecycleKind Lifecycle { get; }

        object Create(ServiceProvider provider);
    }
}
