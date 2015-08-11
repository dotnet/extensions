// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Notification;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotifier(this IServiceCollection services)
        {
#if PROXY_SUPPORT
            services.TryAddSingleton<INotifierMethodAdapter, ProxyNotifierMethodAdapter>();
#else
            services.TryAddSingleton<INotifierMethodAdapter, ReflectionNotifierMethodAdapter>();
#endif
            services.TryAddSingleton<INotifier, Notifier>();
            return services;
        }
    }
}
