// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Provides a mechanism to summarize exceptions for use in telemetry.
/// </summary>
public interface IExceptionSummarizer
{
    /// <summary>
    /// Gives the best available summary of a given <see cref="Exception"/> for telemetry.
    /// </summary>
    /// <param name="exception">The exception to summarize.</param>
    /// <returns>The summary of the given <see cref="Exception"/>.</returns>
    ExceptionSummary Summarize(Exception exception);
}
