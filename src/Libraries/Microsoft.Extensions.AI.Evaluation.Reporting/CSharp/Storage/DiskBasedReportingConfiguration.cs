// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

/// <summary>
/// Contains factory method for creating a <see cref="ReportingConfiguration"/> that persists
/// <see cref="ScenarioRunResult"/>s to disk and also uses the disk to cache AI responses. 
/// </summary>
public static class DiskBasedReportingConfiguration
{
    /// <summary>
    /// Creates a <see cref="ReportingConfiguration"/> that persists <see cref="ScenarioRunResult"/>s to disk and also
    /// uses the disk to cache AI responses.
    /// </summary>
    /// <param name="storageRootPath">
    /// The path to a directory on disk under which the <see cref="ScenarioRunResult"/>s and all cached AI responses
    /// should be stored.
    /// </param>
    /// <param name="evaluators">
    /// The set of <see cref="IEvaluator"/>s that should be invoked to evaluate AI responses.
    /// </param>
    /// <param name="chatConfiguration">
    /// A <see cref="ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that are used by AI-based <paramref name="evaluators"/> included in the
    /// returned <see cref="ReportingConfiguration"/>. Can be omitted if none of the included
    /// <paramref name="evaluators"/> are AI-based.
    /// </param>
    /// <param name="enableResponseCaching">
    /// <see langword="true"/> to enable caching of AI responses; <see langword="false"/> otherwise.
    /// </param>
    /// <param name="cachingKeys">
    /// An optional collection of unique strings that should be hashed when generating the cache keys for cached AI
    /// responses. See <see cref="ReportingConfiguration.CachingKeys"/> for more information about this concept.
    /// </param>
    /// <param name="executionName">
    /// The name of the current execution. See <see cref="ScenarioRun.ExecutionName"/> for more information about this
    /// concept. Uses a fixed default value <c>"Default"</c> if omitted.
    /// </param>
    /// <returns>
    /// A <see cref="ReportingConfiguration"/> that persists <see cref="ScenarioRunResult"/>s to disk and also uses the
    /// disk to cache AI responses.
    /// </returns>
    public static ReportingConfiguration Create(
        string storageRootPath,
        IEnumerable<IEvaluator> evaluators,
        ChatConfiguration? chatConfiguration = null,
        bool enableResponseCaching = true,
        IEnumerable<string>? cachingKeys = null,
        string executionName = Defaults.DefaultExecutionName)
    {
        storageRootPath = Path.GetFullPath(storageRootPath);

        IResponseCacheProvider? responseCacheProvider =
            chatConfiguration is not null && enableResponseCaching
                ? new DiskBasedResponseCacheProvider(storageRootPath)
                : null;

        IResultStore resultStore = new DiskBasedResultStore(storageRootPath);

        return new ReportingConfiguration(
            evaluators,
            resultStore,
            chatConfiguration,
            responseCacheProvider,
            cachingKeys,
            executionName);
    }
}
