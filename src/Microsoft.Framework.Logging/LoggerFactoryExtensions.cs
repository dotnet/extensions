// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// ILoggerFactory extension methods for common scenarios.
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Creates a new ILogger instance using the full name of the given type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="factory">The factory.</param>
        public static ILogger Create<T>(this ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            return factory.Create(typeof(T).FullName);
        }
    }
}