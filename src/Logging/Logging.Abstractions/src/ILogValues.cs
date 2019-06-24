// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides an interface for a state of <see cref="ILogger.Log{TState}"/> that wants to make their values visitable without boxing them to objects.
    /// </summary>
    public interface ILogValues
    {
        /// <summary>
        /// Visits values of the log entry.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The eventId.</param>
        /// <param name="logger">The logger that is able to logging strongly-typed values. It's passed via ref to enable typed wrappers that are structs.</param>
        void Log(LogLevel logLevel, EventId eventId, ITypedLogger logger);
    }
}
