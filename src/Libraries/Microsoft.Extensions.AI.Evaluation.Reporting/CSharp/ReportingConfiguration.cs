// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Represents the configuration for a set of <see cref="ScenarioRun"/>s that defines the set of
/// <see cref="IEvaluator"/>s that should be invoked, the <see cref="Evaluation.ChatConfiguration"/> that should be
/// used by these <see cref="IEvaluator"/>s, how the resulting <see cref="ScenarioRunResult"/>s should be persisted,
/// and how AI responses should be cached.
/// </summary>
public sealed class ReportingConfiguration
{
    /// <summary>
    /// Gets the set of <see cref="IEvaluator"/>s that should be invoked to evaluate AI responses.
    /// </summary>
    public IReadOnlyList<IEvaluator> Evaluators { get; }

    /// <summary>
    /// Gets the <see cref="IResultStore"/> that should be used to persist the <see cref="ScenarioRunResult"/>s.
    /// </summary>
    public IResultStore ResultStore { get; }

    /// <summary>
    /// Gets a <see cref="Evaluation.ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that are used by AI-based <see cref="Evaluators"/> included in this
    /// <see cref="ReportingConfiguration"/>.
    /// </summary>
    public ChatConfiguration? ChatConfiguration { get; }

    /// <summary>
    /// Gets the <see cref="IResponseCacheProvider"/> that should be used to cache AI responses.
    /// </summary>
    public IResponseCacheProvider? ResponseCacheProvider { get; }

    /// <summary>
    /// Gets the collection of unique strings that should be hashed when generating the cache keys for cached AI
    /// responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If no additional caching keys are supplied, then the cache keys for a cached response are generated based on
    /// the content of the AI request that produced this response, metadata such as model name and endpoint present in
    /// the configured <see cref="IChatClient"/> and the <see cref="ChatOptions"/> that are supplied as part of
    /// generating the response.
    /// </para>
    /// <para>
    /// Additionally, the name of the scenario and the iteration are always included in the cache key. This means that
    /// the cached responses for a particular scenario and iteration will not be reused for a different scenario and
    /// iteration even if the AI request content and metadata happen to be the same.
    /// </para>
    /// <para>
    /// Supplying additional caching keys can be useful when some external factors need to be considered when deciding
    /// whether a cached AI response is still valid. For example, consider the case where one of the supplied
    /// additional caching keys is the version of the AI model being invoked. If the product moves to a newer version
    /// of the model, then updating the caching key to reflect this change will cause all cached entries that rely on
    /// this caching key to be invalidated thereby ensuring that the subsequent evaluations will not use the outdated
    /// cached responses produced by the previous model version.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> CachingKeys { get; }

    /// <summary>
    /// Gets the name of the current execution.
    /// </summary>
    /// <remarks>
    /// See <see cref="ScenarioRun.ExecutionName"/> for more information about this concept.
    /// </remarks>
    public string ExecutionName { get; }

    /// <summary>
    /// Gets a function that can be optionally used to override <see cref="EvaluationMetricInterpretation"/>s for
    /// <see cref="EvaluationMetric"/>s returned from evaluations that use this <see cref="ReportingConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// The supplied function can either return a new <see cref="EvaluationMetricInterpretation"/> for any
    /// <see cref="EvaluationMetric"/> that is supplied to it, or return <see langword="null"/> if the
    /// <see cref="EvaluationMetric.Interpretation"/> should be left unchanged.
    /// </remarks>
    public Func<EvaluationMetric, EvaluationMetricInterpretation?>? EvaluationMetricInterpreter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportingConfiguration"/> class.
    /// </summary>
    /// <param name="evaluators">
    /// The set of <see cref="IEvaluator"/>s that should be invoked to evaluate AI responses.
    /// </param>
    /// <param name="resultStore">
    /// The <see cref="IResultStore"/> that should be used to persist the <see cref="ScenarioRunResult"/>s.
    /// </param>
    /// <param name="chatConfiguration">
    /// A <see cref="Evaluation.ChatConfiguration"/> that specifies the <see cref="IChatClient"/> and the
    /// <see cref="IEvaluationTokenCounter"/> that are used by AI-based <paramref name="evaluators"/> included in this
    /// <see cref="ReportingConfiguration"/>. Can be omitted if none of the included <paramref name="evaluators"/> are
    /// AI-based.
    /// </param>
    /// <param name="responseCacheProvider">
    /// The <see cref="IResponseCacheProvider"/> that should be used to cache AI responses. If omitted, AI responses
    /// will not be cached.
    /// </param>
    /// <param name="cachingKeys">
    /// An optional collection of unique strings that should be hashed when generating the cache keys for cached AI
    /// responses. See <see cref="CachingKeys"/> for more information about this concept.
    /// </param>
    /// <param name="executionName">
    /// The name of the current execution. See <see cref="ScenarioRun.ExecutionName"/> for more information about this
    /// concept. Uses a fixed default value <c>"Default"</c> if omitted.
    /// </param>
    /// <param name="evaluationMetricInterpreter">
    /// An optional function that can be used to override <see cref="EvaluationMetricInterpretation"/>s for
    /// <see cref="EvaluationMetric"/>s returned from evaluations that use this <see cref="ReportingConfiguration"/>.
    /// The supplied function can either return a new <see cref="EvaluationMetricInterpretation"/> for any
    /// <see cref="EvaluationMetric"/> that is supplied to it, or return <see langword="null"/> if the
    /// <see cref="EvaluationMetric.Interpretation"/> should be left unchanged.
    /// </param>
    public ReportingConfiguration(
        IEnumerable<IEvaluator> evaluators,
        IResultStore resultStore,
        ChatConfiguration? chatConfiguration = null,
        IResponseCacheProvider? responseCacheProvider = null,
        IEnumerable<string>? cachingKeys = null,
        string executionName = Defaults.DefaultExecutionName,
        Func<EvaluationMetric, EvaluationMetricInterpretation?>? evaluationMetricInterpreter = null)
    {
        Evaluators = [.. evaluators];
        ResultStore = resultStore;
        ChatConfiguration = chatConfiguration;
        ResponseCacheProvider = responseCacheProvider;

        cachingKeys ??= [];
        if (chatConfiguration is not null)
        {
            cachingKeys = cachingKeys.Concat(GetCachingKeysForChatClient(chatConfiguration.ChatClient));
        }

        CachingKeys = [.. cachingKeys];
        ExecutionName = executionName;
        EvaluationMetricInterpreter = evaluationMetricInterpreter;
    }

    /// <summary>
    /// Creates a new <see cref="ScenarioRun"/> with the specified <paramref name="scenarioName"/> and
    /// <paramref name="iterationName"/>.
    /// </summary>
    /// <param name="scenarioName">The <see cref="ScenarioRun.ScenarioName"/>.</param>
    /// <param name="iterationName">
    /// The <see cref="ScenarioRun.IterationName"/>. Uses default value <c>"1"</c> if omitted.
    /// </param>
    /// <param name="additionalCachingKeys">
    /// An optional collection of unique strings that should be hashed when generating the cache keys for cached AI
    /// responses. See <see cref="CachingKeys"/> for more information about this concept.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the operation.</param>
    /// <returns>
    /// A new <see cref="ScenarioRun"/> with the specified <paramref name="scenarioName"/> and
    /// <paramref name="iterationName"/>.
    /// </returns>
    public async ValueTask<ScenarioRun> CreateScenarioRunAsync(
        string scenarioName,
        string iterationName = Defaults.DefaultIterationName,
        IEnumerable<string>? additionalCachingKeys = null,
        CancellationToken cancellationToken = default)
    {
        ChatConfiguration? chatConfiguration = ChatConfiguration;

        if (chatConfiguration is not null && ResponseCacheProvider is not null)
        {
            IChatClient originalChatClient = chatConfiguration.ChatClient;

            IEnumerable<string> cachingKeys =
                additionalCachingKeys is null
                    ? [scenarioName, iterationName, .. CachingKeys]
                    : [scenarioName, iterationName, .. CachingKeys, .. additionalCachingKeys];

            IDistributedCache cache =
                await ResponseCacheProvider.GetCacheAsync(
                    scenarioName,
                    iterationName,
                    cancellationToken).ConfigureAwait(false);

#pragma warning disable CA2000
            // CA2000: Dispose objects before they go out of scope.
            // ResponseCachingChatClient is a wrapper around the IChatClient supplied by the caller. Disposing
            // ResponseCachingChatClient would also dispose the IChatClient supplied by the caller. Disposing this
            // within the evaluation library is problematic because the caller would then lose control over the
            // lifetime of the supplied IChatClient. We disable this warning because we want to give the caller
            // complete control over the lifetime of the supplied IChatClient.

            var cachingChatClient =
                new ResponseCachingChatClient(
                    originalChatClient,
                    cache,
                    cachingKeys);
#pragma warning restore CA2000

            chatConfiguration = new ChatConfiguration(cachingChatClient, chatConfiguration.TokenCounter);
        }

        return new ScenarioRun(
            scenarioName,
            iterationName,
            ExecutionName,
            Evaluators,
            ResultStore,
            chatConfiguration,
            EvaluationMetricInterpreter);
    }

    private static IEnumerable<string> GetCachingKeysForChatClient(IChatClient chatClient)
    {
        var metadata = chatClient.GetService<ChatClientMetadata>();

        string? providerName = metadata?.ProviderName;
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            yield return providerName!;
        }

        Uri? providerUri = metadata?.ProviderUri;
        if (providerUri is not null)
        {
            yield return providerUri.AbsoluteUri;
        }

        string? modelId = metadata?.ModelId;
        if (!string.IsNullOrWhiteSpace(modelId))
        {
            yield return modelId!;
        }
    }
}
