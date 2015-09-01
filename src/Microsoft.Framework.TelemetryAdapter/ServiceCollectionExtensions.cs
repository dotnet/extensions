// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.TelemetryAdapter;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTelemetrySourceAdapter(this IServiceCollection services)
        {
#if PROXY_SUPPORT
            services.TryAddSingleton<ITelemetrySourceMethodAdapter, ProxyTelemetrySourceMethodAdapter>();
#else
            services.TryAddSingleton<ITelemetrySourceMethodAdapter, ReflectionTelemetrySourceMethodAdapter>();
#endif
            services.TryAddSingleton<TelemetrySourceAdapter, DefaultTelemetrySourceAdapter>();
            return services;
        }
    }
}
