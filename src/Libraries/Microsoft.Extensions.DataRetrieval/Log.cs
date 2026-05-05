// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.DataRetrieval
{
    internal static partial class Log
    {
        [LoggerMessage(0, LogLevel.Debug, "Running query processor: {processor}.")]
        internal static partial void RunningQueryProcessor(this ILogger logger, string processor);

        [LoggerMessage(1, LogLevel.Debug, "Searching variant: {variant}.")]
        internal static partial void SearchingVariant(this ILogger logger, string variant);

        [LoggerMessage(2, LogLevel.Debug, "Running result processor: {processor}.")]
        internal static partial void RunningResultProcessor(this ILogger logger, string processor);
    }
}
