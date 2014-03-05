using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public class ServiceDescriptor : IServiceDescriptor
    {
        public LifecycleKind Lifecycle { get; set;  }
        public Type ServiceType { get; set;  }

        // Exactly one of the two following properties should be set
        public Type ImplementationType { get; set; } // nullable
        public object ImplementationInstance { get; set; } // nullable
    }
}
