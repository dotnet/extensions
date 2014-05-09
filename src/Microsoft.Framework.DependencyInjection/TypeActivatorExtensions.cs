// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class TypeActivatorExtensions
    {
        public static T CreateInstance<T>(this ITypeActivator activator, IServiceProvider serviceProvider, params object[] parameters)
        {
            if (activator == null)
            {
                throw new ArgumentNullException("activator");
            }

            return (T)activator.CreateInstance(serviceProvider, typeof(T), parameters);
        }
    }
}