// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Represents a component that samples log records.
/// </summary>
public interface ILoggingSampler
{
    /// <summary>
    /// Sample a log record if it matches the <paramref name="logRecordPattern"/>.
    /// </summary>
    /// <param name="logRecordPattern">A log record pattern to match against.</param>
    /// <returns>True, if the log record was sampled. False otherwise.</returns>
    public bool Sample(LogRecordPattern logRecordPattern);
}
