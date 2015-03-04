// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging.Internal;

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
        private static readonly Func<object, Exception, string> _logValuesFormatter = (state, error)
            => LogValuesFormatter((ILogValues)state, error);

        //------------------------------------------DEBUG------------------------------------------//

        /// <summary>
        /// Writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="data">The message to log.</param>
        // FYI, this field is called data because naming it message triggers CA1303 and CA2204 for callers.
        public static void WriteDebug([NotNull] this ILogger logger, string data)
        {
            logger.Log(LogLevel.Debug, 0, data, null, TheMessage);
        }

        /// <summary>
        /// Writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="data">The message to log.</param>
        public static void WriteDebug([NotNull] this ILogger logger, int eventId, string data)
        {
            logger.Log(LogLevel.Debug, eventId, data, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteDebug([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Debug, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteDebug([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Debug, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteDebug(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Debug, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteDebug(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Debug, eventId, state, error);
        }

        //------------------------------------------VERBOSE------------------------------------------//

        /// <summary>
        /// Writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="data">The message to log.</param>
        // FYI, this field is called data because naming it message triggers CA1303 and CA2204 for callers.
        public static void WriteVerbose([NotNull] this ILogger logger, string data)
        {
            logger.Log(LogLevel.Verbose, 0, data, null, TheMessage);
        }

        /// <summary>
        /// Writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="data">The message to log.</param>
        public static void WriteVerbose([NotNull] this ILogger logger, int eventId, string data)
        {
            logger.Log(LogLevel.Verbose, eventId, data, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteVerbose([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Verbose, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteVerbose([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Verbose, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteVerbose(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Verbose, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a verbose log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteVerbose(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Verbose, eventId, state, error);
        }

        //------------------------------------------INFORMATION------------------------------------------//

        /// <summary>
        /// Writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteInformation([NotNull] this ILogger logger, string message)
        {
            logger.Log(LogLevel.Information, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteInformation([NotNull] this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Information, eventId, message, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteInformation([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Information, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteInformation([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Information, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteInformation(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Information, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes an informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteInformation(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Information, eventId, state, error);
        }

        //------------------------------------------WARNING------------------------------------------//

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteWarning([NotNull] this ILogger logger, string message)
        {
            logger.Log(LogLevel.Warning, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteWarning([NotNull] this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Warning, eventId, message, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteWarning([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Warning, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteWarning([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Warning, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given message and error and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteWarning([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Log(LogLevel.Warning, 0, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given message and error and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteWarning([NotNull] this ILogger logger, int eventId, string message, Exception error)
        {
            logger.Log(LogLevel.Warning, eventId, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteWarning(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Warning, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteWarning(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Warning, eventId, state, error);
        }

        //------------------------------------------ERROR------------------------------------------//

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteError([NotNull] this ILogger logger, string message)
        {
            logger.Log(LogLevel.Error, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteError([NotNull] this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Error, eventId, message, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteError([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Error, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteError([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Error, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given message and error and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteError([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Log(LogLevel.Error, 0, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given message and error and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteError([NotNull] this ILogger logger, int eventId, string message, Exception error)
        {
            logger.Log(LogLevel.Error, eventId, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteError(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Error, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes an error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteError(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Error, eventId, state, error);
        }

        //------------------------------------------CRITICAL------------------------------------------//

        /// <summary>
        /// Writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteCritical([NotNull] this ILogger logger, string message)
        {
            logger.Log(LogLevel.Critical, 0, message, null, TheMessage);
        }

        /// <summary>
        /// Writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        public static void WriteCritical([NotNull] this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Critical, eventId, message, null, TheMessage);
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteCritical([NotNull] this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Critical, 0, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="format">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void WriteCritical([NotNull] this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Critical, eventId, new FormattedLogValues(format, args), null, _logValuesFormatter);
        }

        /// <summary>
        /// Formats the given message and error and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteCritical([NotNull] this ILogger logger, string message, Exception error)
        {
            logger.Log(LogLevel.Critical, 0, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given message and error and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteCritical([NotNull] this ILogger logger, int eventId, string message, Exception error)
        {
            logger.Log(LogLevel.Critical, eventId, message, error, TheMessageAndError);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteCritical(
            [NotNull] this ILogger logger,
            ILogValues state,
            Exception error = null)
        {
            logger.Log(LogLevel.Critical, state, error);
        }

        /// <summary>
        /// Formats the given <see cref="ILogValues"/> and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="state">The <see cref="ILogValues"/> to write.</param>
        /// <param name="error">The exception to log.</param>
        public static void WriteCritical(
            [NotNull] this ILogger logger,
            int eventId,
            ILogValues state,
            Exception error = null)
        {
            logger.WriteWithEvent(LogLevel.Critical, eventId, state, error);
        }

        //------------------------------------------HELPERS------------------------------------------//

        private static void Log(
            this ILogger logger,
            LogLevel logLevel,
            ILogValues state,
            Exception exception = null)
        {
            logger.Log(logLevel, 0, state, exception, _logValuesFormatter);
        }

        private static void WriteWithEvent(
            this ILogger logger,
            LogLevel logLevel,
            int eventId,
            ILogValues state,
            Exception exception = null)
        {
            logger.Log(logLevel, eventId, state, exception, _logValuesFormatter);
        }

        private static string LogValuesFormatter(ILogValues state, Exception exception)
        {
            if (exception == null)
            {
                return state.Format();
            }

            return state.Format() + Environment.NewLine + exception;
        }
    }
}