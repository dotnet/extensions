// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging.Debug;

namespace Microsoft.Framework.Logging
{
    public static class DebugLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        public static ILoggerFactory AddDebug(this ILoggerFactory factory)
        {
            return AddDebug(
                factory,
                (category, logLevel) => logLevel >= LogLevel.Information);
        }

        /// <summary>
        /// Adds a debug logger that is enabled as defined by the filter function.
        /// </summary>
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter)
        {
            factory.AddProvider(new DebugLoggerProvider(filter));
            return factory;
        }

        /// <summary>
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel)
        {
            return AddDebug(
               factory,
               (category, logLevel) => logLevel >= minLevel);
        }
    }
}