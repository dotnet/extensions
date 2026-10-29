// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Selects the retry defaults applied by the standard resilience handler.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
public enum HttpRetryPreset
{
    /// <summary>
    /// The default retry behavior of <see cref="HttpStandardResilienceOptions.Retry"/>, without any changes.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Retry defaults that reduce the extra load that retries put on a degraded dependency.
    /// </summary>
    /// <remarks>
    /// Compared to <see cref="Standard"/>:
    /// <list type="bullet">
    /// <item><description><see cref="Polly.Retry.RetryStrategyOptions{TResult}.MaxRetryAttempts"/> is 2 instead of 3.</description></item>
    /// <item><description><see cref="HttpRetryStrategyOptions.RetryBudget"/> is enabled with the defaults of <see cref="HttpRetryBudgetOptions"/>.</description></item>
    /// </list>
    /// </remarks>
    Budgeted = 1,
}
