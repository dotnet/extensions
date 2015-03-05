// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            return new ServiceProvider(services);
        }

        public static IServiceCollection AddTypeActivator([NotNull]this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<ITypeActivator, TypeActivator>());
            return services;
        }
    }
}
