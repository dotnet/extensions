// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || DNX451 || DNXCORE50

using Microsoft.Framework.Notification;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotifier(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton(typeof(INotifierMethodAdapter), typeof(NotifierMethodAdapter)));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(INotifier), typeof(Notifier)));
            return services;
        }
    }
}

#endif
