// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// The interface implemented by components which know how to summarize exceptions.
/// </summary>
/// <remarks>
/// This is the interface implemented by summary providers which are consumed by the higher-level
/// summarization components. To receive summary information, applications use
/// <see cref="T:Microsoft.Extensions.Diagnostics.ExceptionSummarization.IExceptionSummarizer" /> instead.
/// </remarks>
public interface IExceptionSummaryProvider
{
    /// <summary>
    /// Gets the set of supported exception types that can be handled by this provider.
    /// </summary>
    IEnumerable<Type> SupportedExceptionTypes { get; }

    /// <summary>
    /// Gets the set of description strings exposed by this provider.
    /// </summary>
    IReadOnlyList<string> Descriptions { get; }

    /// <summary>
    /// Provides the index of the description for the exception along with optional additional data.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="additionalDetails">The additional details of the given exception, if any.</param>
    /// <returns>The index of the description.</returns>
    /// <remarks>
    /// This method should only get invoked with an exception which is type compatible with a type
    /// described by <see cref="P:Microsoft.Extensions.Diagnostics.ExceptionSummarization.IExceptionSummaryProvider.SupportedExceptionTypes" />.
    /// </remarks>
    int Describe(Exception exception, out string? additionalDetails);
}
