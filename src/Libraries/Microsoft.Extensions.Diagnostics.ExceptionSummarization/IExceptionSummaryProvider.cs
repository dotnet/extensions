// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// The interface implemented by components which know how to summarize exceptions.
/// </summary>
/// <remarks>
/// This is the interface implemented by summary providers which are consumed by the higher-level
/// summarization components. To receive summary information, applications use
/// <see cref="IExceptionSummarizer"/> instead.
/// </remarks>
public interface IExceptionSummaryProvider
{
    /// <summary>
    /// Provides the index of the description for the exception along with optional additional data.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="additionalDetails">The additional details of the given exception, if any. Ideally, this string should not contain any privacy-sensitive information.</param>
    /// <returns>The index of the description.</returns>
    /// <remarks>
    /// This method should only get invoked with an exception which is type compatible with a type
    /// described by <see cref="SupportedExceptionTypes"/>.
    /// </remarks>
    int Describe(Exception exception, out string? additionalDetails);

    /// <summary>
    /// Gets the set of supported exception types that can be handled by this provider.
    /// </summary>
    IEnumerable<Type> SupportedExceptionTypes { get; }

    /// <summary>
    /// Gets the set of description strings exposed by this provider.
    /// </summary>
    IReadOnlyList<string> Descriptions { get; }
}
