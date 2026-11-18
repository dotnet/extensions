// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for <see cref="IDecisionClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DecisionClientExtensions
{
    /// <summary>Asks the <see cref="IDecisionClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    public static TService? GetService<TService>(this IDecisionClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);
        return client.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>Evaluates application-owned state against an ordered batch of questions.</summary>
    /// <typeparam name="TState">The application state type.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="state">The application-owned state.</param>
    /// <param name="stateTypeInfo">The JSON contract used to serialize <paramref name="state"/>.</param>
    /// <param name="questions">The ordered heterogeneous question batch.</param>
    /// <param name="contents">Optional ordered content evaluated together with the state.</param>
    /// <param name="options">Optional per-request options.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>The response, with one answer per question in request order.</returns>
    public static async Task<DecisionResponse> GetDecisionAsync<TState>(
        this IDecisionClient client,
        TState state,
        JsonTypeInfo<TState> stateTypeInfo,
        IEnumerable<DecisionQuestion> questions,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(stateTypeInfo);
        _ = Throw.IfNull(questions);

        DecisionRequest request = new(
            JsonSerializer.SerializeToElement(state, stateTypeInfo),
            questions.ToArray(),
            contents?.ToArray());

        return await client.GetDecisionAsync(request, options, cancellationToken).ConfigureAwait(false);
    }
}
