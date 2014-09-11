// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.DependencyInjection
{
#if ASPNET50 || ASPNETCORE50
    [Microsoft.Framework.Runtime.AssemblyNeutral]
#endif
    public interface IServiceCollection : IEnumerable<IServiceDescriptor>
    {
        IServiceCollection Add(IServiceDescriptor descriptor);

        IServiceCollection Add(IEnumerable<IServiceDescriptor> descriptors);

        IServiceCollection AddTransient(Type service, Type implementationType);

        IServiceCollection AddScoped(Type service, Type implementationType);

        IServiceCollection AddSingleton(Type service, Type implementationType);

        IServiceCollection AddInstance(Type service, object implementationInstance);
    }
}
