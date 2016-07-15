// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal interface IService
    {
        IService Next { get; set; }

        ServiceLifetime Lifetime { get; }

        IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain);

        Type ServiceType { get; }
    }
}
