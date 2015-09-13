// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.TelemetryAdapter;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up telemetry source adapter related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class TelemetrySourceAdapterServiceCollectionExtensions
    {
        /// <summary>
        /// Adds telemetry source adapter services to the specified <see cref="IServiceCollection" />. 
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
