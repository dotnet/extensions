// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging.Console
{
    public static class ConsoleLoggerExtensions
    {
        /// <summary>
        /// Adds a console logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        public static ILoggerFactory AddConsole(this ILoggerFactory factory)
        {
            factory.AddProvider(new ConsoleLoggerProvider((category, logLevel) => logLevel >= LogLevel.Information));
            return factory;
        }

        /// <summary>
        /// Adds a console logger that is enabled as defined by the filter function.
        /// </summary>
        public static ILoggerFactory AddConsole(this ILoggerFactory factory, Func<string, LogLevel, bool> filter)
        {
            factory.AddProvider(new ConsoleLoggerProvider(filter));
            return factory;
        }

        /// <summary>
        /// Adds a console logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddConsole(this ILoggerFactory factory, LogLevel minLevel)
        {
            factory.AddProvider(new ConsoleLoggerProvider((category, logLevel) => logLevel >= minLevel));
            return factory;
        }
    }
}