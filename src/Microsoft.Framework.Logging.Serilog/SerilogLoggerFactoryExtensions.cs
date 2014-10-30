// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog;
using Microsoft.Framework.Logging.Serilog;
using JetBrains.Annotations;

namespace Microsoft.Framework.Logging
{
    public static class SerilogLoggerFactoryExtensions
    {
        public static ILoggerFactory AddSerilog(
            [NotNull] this ILoggerFactory factory, [NotNull] LoggerConfiguration loggerConfiguration)
        {
            factory.AddProvider(new SerilogLoggerProvider(loggerConfiguration));

            return factory;
        }
    }
}