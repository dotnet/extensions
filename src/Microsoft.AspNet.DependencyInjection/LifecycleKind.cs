using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.DependencyInjection
{
    //[AssemblyNeutral]
    public enum LifecycleKind
    {
        Singleton,
        Scoped,
        Transient
    }
}