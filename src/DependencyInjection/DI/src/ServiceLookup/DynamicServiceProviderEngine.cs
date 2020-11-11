// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class DynamicServiceProviderEngine : CompiledServiceProviderEngine
    {
        public DynamicServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback) : base(serviceDescriptors, callback)
        {
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            var callCount = 0;
            return scope =>
            {
                // Resolve the result before we increment the call count, this ensures that singletons
                // won't cause any side effects during the compilation of the resolve function.
                var result = RuntimeResolver.Resolve(callSite, scope);

                if (Interlocked.Increment(ref callCount) == 2)
                {
                    // Don't capture the ExecutionContext when forking to build the compiled version of the
                    // resolve function
                    ThreadPool.UnsafeQueueUserWorkItem(state =>
                    {
                        try
                        {
                            base.RealizeService(callSite);
                        }
                        catch
                        {
                            // Swallow the exception, we should log this via the event source in a non-patched release
                        }
                    },
                    null);
                }

                return result;
            };
        }
    }
}
