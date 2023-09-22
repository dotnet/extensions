// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Provides a mechanism to summarize exceptions for use in telemetry.
/// </summary>
public interface IExceptionSummarizer
{
    /// <summary>
    /// Gives the best available summary of a given <see cref="T:System.Exception" /> for telemetry.
    /// </summary>
    /// <param name="exception">The exception to summarize.</param>
    /// <returns>The summary of the given <see cref="T:System.Exception" />.</returns>
    ExceptionSummary Summarize(Exception exception);
}
