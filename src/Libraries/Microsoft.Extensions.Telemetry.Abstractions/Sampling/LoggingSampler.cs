// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Controls the number of samples of log records collected and sent to the backend.
/// </summary>
public abstract class LoggingSampler
{
    /// <summary>
    /// Makes a sampling decision for the provided <paramref name="logEntry"/>.
    /// </summary>
    /// <param name="logEntry">The log entry used to make the sampling decision for.</param>
    /// <typeparam name="TState">The type of the log entry state.</typeparam>
    /// <returns><see langword="true" /> if the log record should be sampled; otherwise, <see langword="false" />.</returns>
    public abstract bool ShouldSample<TState>(in LogEntry<TState> logEntry);
}
