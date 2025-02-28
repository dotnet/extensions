// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Provides a way to get the <see cref="IDistributedCache"/> that caches the AI responses associated with a particular
/// <see cref="ScenarioRun"/>.
/// </summary>
/// <remarks>
/// <see cref="IResponseCacheProvider"/> can be used to set up caching of AI-generated responses (both the AI responses
/// under evaluation as well as the AI responses for the evaluations themselves). When caching is enabled, the AI
/// responses associated with each <see cref="ScenarioRun"/> are stored in the <see cref="IDistributedCache"/> that is
/// returned from this <see cref="IResponseCacheProvider"/>. So long as the inputs (such as the content included in the
/// requests, the AI model being invoked etc.) remain unchanged, subsequent evaluations of the same
/// <see cref="ScenarioRun"/> use the cached responses instead of invoking the AI model to generate new ones. Bypassing
/// the AI model when the inputs remain unchanged results in faster execution at a lower cost.
/// </remarks>
public interface IResponseCacheProvider
{
    /// <summary>
    /// Returns an <see cref="IDistributedCache"/> that caches the AI responses associated with a particular
    /// <see cref="ScenarioRun"/>.
    /// </summary>
    /// <param name="scenarioName">The <see cref="ScenarioRun.ScenarioName"/>.</param>
    /// <param name="iterationName">The <see cref="ScenarioRun.IterationName"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// An <see cref="IDistributedCache"/> that caches the AI responses associated with a particular
    /// <see cref="ScenarioRun"/>.
    /// </returns>
    ValueTask<IDistributedCache> GetCacheAsync(
        string scenarioName,
        string iterationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes cached AI responses for all <see cref="ScenarioRun"/>s.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired cache entries for all <see cref="ScenarioRun"/>s.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask DeleteExpiredCacheEntriesAsync(CancellationToken cancellationToken = default);
}
