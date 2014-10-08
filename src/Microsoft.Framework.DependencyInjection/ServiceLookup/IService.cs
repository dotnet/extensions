// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    internal interface IService
    {
        IService Next { get; set; }

        LifecycleKind Lifecycle { get; }

        object Create(ServiceProvider provider);

        IServiceCallSite CreateCallSite(ServiceProvider provider);
    }
}
