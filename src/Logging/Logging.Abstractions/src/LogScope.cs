// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Represents a log message which is pre-computed and strongly typed to reduce logging overhead.
    /// </summary>
    public struct LogScope
    {
        private readonly Func<ILogger, IDisposable> _scope;

        /// <summary>
        /// Initializes an instance of the <see cref="LogScope"/> struct.
        /// </summary>
        /// <param name="formatString">The scope format string</param>
        public LogScope(string formatString)
        {
            FormatString = formatString;
            _scope = LoggerMessage.DefineScope(formatString);
        }

        /// <summary>
        /// Gets the format string of this log scope.
        /// </summary>
        public string FormatString { get; }

        /// <summary>
        /// Begins a structured log scope on registered providers.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        public IDisposable Begin(ILogger logger)
        {
            return _scope(logger);
        }

        /// <summary>
        /// Implicitly initialize the <see cref="LogScope"/> from the given parameters.
        /// </summary>
        /// <param name="formatString">The format string to initialize the <see cref="LogScope"/> struct.</param>
        public static implicit operator LogScope(string formatString)
        {
            return new LogScope(formatString);
        }
    }

    /// <summary>
    /// Represents a log message which is pre-computed and strongly typed to reduce logging overhead.
    /// </summary>
    /// <typeparam name="T1">The type of the value in the first position of the format string.</typeparam>
    public struct LogScope<T1>
    {
        private readonly Func<ILogger, T1, IDisposable> _scope;

        /// <summary>
        /// Initializes an instance of the <see cref="LogScope{T1}"/> struct.
        /// </summary>
        /// <param name="formatString">The scope format string</param>
        public LogScope(string formatString)
        {
            FormatString = formatString;
            _scope = LoggerMessage.DefineScope<T1>(formatString);
        }

        /// <summary>
        /// Gets the format string of this log scope.
        /// </summary>
        public string FormatString { get; }

        /// <summary>
        /// Begins a structured log scope on registered providers.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="value1">The value at the first position in the format string.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        public IDisposable Begin(ILogger logger, T1 value1)
        {
            return _scope(logger, value1);
        }

        /// <summary>
        /// Implicitly initialize the <see cref="LogScope{T1}"/> from the given parameters.
        /// </summary>
        /// <param name="formatString">The format string to initialize the <see cref="LogScope{T1}"/> struct.</param>
        public static implicit operator LogScope<T1>(string formatString)
        {
            return new LogScope<T1>(formatString);
        }
    }

    /// <summary>
    /// Represents a log message which is pre-computed and strongly typed to reduce logging overhead.
    /// </summary>
    /// <typeparam name="T1">The type of the value in the first position of the format string.</typeparam>
    /// <typeparam name="T2">The type of the value in the second position of the format string.</typeparam>
    public struct LogScope<T1, T2>
    {
        private readonly Func<ILogger, T1, T2, IDisposable> _scope;

        /// <summary>
        /// Initializes an instance of the <see cref="LogScope{T1, T2}"/> struct.
        /// </summary>
        /// <param name="formatString">The scope format string</param>
        public LogScope(string formatString)
        {
            FormatString = formatString;
            _scope = LoggerMessage.DefineScope<T1, T2>(formatString);
        }

        /// <summary>
        /// Gets the format string of this log scope.
        /// </summary>
        public string FormatString { get; }

        /// <summary>
        /// Begins a structured log scope on registered providers.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="value1">The value at the first position in the format string.</param>
        /// <param name="value2">The value at the second position in the format string.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        public IDisposable Begin(ILogger logger, T1 value1, T2 value2)
        {
            return _scope(logger, value1, value2);
        }

        /// <summary>
        /// Implicitly initialize the <see cref="LogScope{T1, T2}"/> from the given parameters.
        /// </summary>
        /// <param name="formatString">The format string to initialize the <see cref="LogScope{T1, T2}"/> struct.</param>
        public static implicit operator LogScope<T1, T2>(string formatString)
        {
            return new LogScope<T1, T2>(formatString);
        }
    }

}
