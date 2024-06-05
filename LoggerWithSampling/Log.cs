// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;
using Microsoft.Extensions.Logging;

namespace ConsoleLogger
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Learn more about \"{topic}\" at \"{docLocation}\"")]
        public static partial void LearnMoreAt(this ILogger logger, string topic, string docLocation);

        [LoggerMessage(2, LogLevel.Error, "An exception of type ArgumentNullException was thrown")]
        public static partial void LogArgumentNullException(this ILogger logger, Exception exception);
    }
}
