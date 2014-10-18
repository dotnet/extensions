// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging.Console
{
    public static class ConsoleLoggerExtensions
    {
        /// <summary>
        /// Adds a console logger that is enabled for severity levels of warning or higher.
        /// </summary>
        public static ILoggerFactory AddConsole(this ILoggerFactory factory)
        {
            factory.AddProvider(new ConsoleLoggerProvider((category, traceType) => traceType >= TraceType.Information));
            return factory;
        }

        /// <summary>
        /// Adds a console logger that is enabled as defined by the filter function.
        /// </summary>
        public static ILoggerFactory AddConsole(this ILoggerFactory factory, Func<string, TraceType, bool> filter)
        {
            factory.AddProvider(new ConsoleLoggerProvider(filter));
            return factory;
        }
    }
}