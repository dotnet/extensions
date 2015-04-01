// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Used to create logger instances of the given name.
    /// </summary>
    public interface ILoggerFactory : IDisposable
    {
        /// <summary>
        /// The minimum level of log messages sent to registered loggers.
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Creates a new ILogger instance of the given name.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        ILogger CreateLogger(string categoryName);

        void AddProvider(ILoggerProvider provider);
    }
}