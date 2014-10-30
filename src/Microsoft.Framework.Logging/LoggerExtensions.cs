// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// ILogger extension methods for common scenarios.
    /// </summary>
    public static class LoggerExtensions
    {
        private static readonly Func<object, Exception, string> TheMessage = (message, error) => (string)message;
        private static readonly Func<object, Exception, string> TheMessageAndError = (message, error)
            => string.Format(CultureInfo.CurrentCulture, "{0}{1}{2}", message, Environment.NewLine, error);
        private static readonly Func<object, Exception, string> _loggerStructureFormatter = (state, error) 
            => LoggerStructureFormatter((LoggerStructureBase) state, error);

        /// <summary>
        /// Writes a verbose log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="data"></param>
        // FYI, this field is called data because naming it message triggers CA1303 and CA2204 for callers.
        public static void WriteVerbose([NotNull] this ILogger logger, string data)
        {
            logger.Write(LogLevel.Verbose, 0, data, null, TheMessage);
        }

        public static void WriteVerbose(
            [NotNull] this ILogger logger, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(LogLevel.Verbose, message, exception);
        }

        /// <summary>
        /// Writes an informational log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void WriteInformation([NotNull] this ILogger logger, string message)
        {
            logger.Write(LogLevel.Information, 0, message, null, TheMessage);
        }

        public static void WriteInformation(
            [NotNull] this ILogger logger, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(LogLevel.Information, message, exception);
        }

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void WriteWarning([NotNull] this ILogger logger, string message, params string[] args)
        {
            logger.Write(LogLevel.Warning, 0,
                string.Format(CultureInfo.InvariantCulture, message, args), null, TheMessage);
        }

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public static void WriteWarning([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Write(LogLevel.Warning, 0, message, error, TheMessageAndError);
        }

        public static void WriteWarning(
            [NotNull] this ILogger logger, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void WriteError([NotNull] this ILogger logger, string message)
        {
            logger.Write(LogLevel.Error, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public static void WriteError([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Write(LogLevel.Error, 0, message, error, TheMessageAndError);
        }

        public static void WriteError(
            [NotNull] this ILogger logger, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// Writes a critical log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void WriteCritical([NotNull] this ILogger logger, string message)
        {
            logger.Write(LogLevel.Critical, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes a critical log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public static void WriteCritical([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Write(LogLevel.Critical, 0, message, error, TheMessageAndError);
        }

        public static void WriteCritical(
            [NotNull] this ILogger logger, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(LogLevel.Critical, message, exception);
        }

        private static void Write(
            this ILogger logger, 
            LogLevel logLevel, 
            LoggerStructureBase message, 
            Exception exception = null)
        {
            logger.Write(logLevel, 0, message, null, _loggerStructureFormatter);
        }

        private static string LoggerStructureFormatter(LoggerStructureBase state, Exception exception)
        {
            return state.Format();
        }
    }
}
