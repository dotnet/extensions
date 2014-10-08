using Microsoft.Framework.DependencyInjection.ServiceLookup;
using System;
using System.Linq.Expressions;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for InstanceService
    /// </summary>
    internal class InstanceService : IService
    {
        private readonly IServiceDescriptor _descriptor;

        public InstanceService(IServiceDescriptor descriptor)
        {
            Lifecycle = descriptor.Lifecycle;
            _descriptor = descriptor;
        }

        public IService Next { get; set; }

        public LifecycleKind Lifecycle { get; private set; }
   
        public IServiceCallSite CreateCallSite(ServiceProvider provider)
        {
            return new CallSite(_descriptor);
        }

        private class CallSite : IServiceCallSite
        {
            private readonly IServiceDescriptor _descriptor;

            public CallSite(IServiceDescriptor descriptor)
            {
                _descriptor = descriptor;
            }

            public object Invoke(ServiceProvider provider)
            {
                return _descriptor.ImplementationInstance;
            }

            public Expression Build(Expression provider)
            {
                return Expression.Constant(_descriptor.ImplementationInstance, _descriptor.ServiceType);
            }
        }
    }
}
