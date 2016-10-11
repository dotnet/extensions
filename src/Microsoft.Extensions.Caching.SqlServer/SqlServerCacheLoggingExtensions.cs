// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.SqlServer
{
    internal static class SqlServerCacheLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _suppressedCacheException;

        static SqlServerCacheLoggingExtensions()
        {
            _suppressedCacheException = LoggerMessage.Define(
                LogLevel.Debug,
                0,
                "An exception during cache operation was suppressed");
        }

        public static void ExceptionSuppressed(this ILogger logger, Exception ex)
        {
            _suppressedCacheException(logger, ex);
        }
    }
}
