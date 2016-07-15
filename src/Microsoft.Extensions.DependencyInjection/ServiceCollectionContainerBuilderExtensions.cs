// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionContainerBuilderExtensions
    {
        /// <summary>
        /// Creates an <see cref="IServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing service descriptors.</param>
        /// <returns>The<see cref="IServiceProvider"/>.</returns>

        public static IServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            return BuildServiceProvider(services, validateScopes: false);
        }

        /// <summary>
        /// Creates an <see cref="IServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>
        /// optionaly enabling scope validation.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing service descriptors.</param>
        /// <param name="validateScopes">
        /// <c>true</c> to perform check verifying that scoped services never gets resolved from root provider; otherwise <c>false</c>.
        /// </param>
        /// <returns>The<see cref="IServiceProvider"/>.</returns>
        public static IServiceProvider BuildServiceProvider(this IServiceCollection services, bool validateScopes)
        {
            return new ServiceProvider(services, validateScopes);
        }
    }
}
