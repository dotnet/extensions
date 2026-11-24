// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// <param name="state">The application-owned state, serialized to <see cref="DecisionRequest.State"/>.</param>
    /// <param name="questions">The ordered heterogeneous question batch.</param>
    /// <param name="contents">Optional ordered content evaluated together with the state.</param>
    /// <param name="options">Optional per-request options.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>The response, with one answer per question in request order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> is an <see cref="AIContent"/> or a sequence of <see cref="AIContent"/>.</exception>
    /// <remarks>
    /// <paramref name="state"/> is serialized using <see cref="AIJsonUtilities.DefaultOptions"/>. For Native AOT, use the overload that accepts
    /// <see cref="JsonSerializerOptions"/> with options from a source-generated context.
    /// </remarks>
    public static Task<DecisionResponse> GetDecisionAsync<TState>(
        this IDecisionClient client,
        TState state,
        IEnumerable<DecisionQuestion> questions,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        GetDecisionAsync(client, state, AIJsonUtilities.DefaultOptions, questions, contents, options, cancellationToken);

    /// <summary>Evaluates application-owned state against an ordered batch of questions.</summary>
    /// <typeparam name="TState">The application state type.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="state">The application-owned state, serialized to <see cref="DecisionRequest.State"/>.</param>
    /// <param name="serializerOptions">The JSON serialization options used to serialize <paramref name="state"/>.</param>
    /// <param name="questions">The ordered heterogeneous question batch.</param>
    /// <param name="contents">Optional ordered content evaluated together with the state.</param>
    /// <param name="options">Optional per-request options.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>The response, with one answer per question in request order.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="client"/>, <paramref name="serializerOptions"/>, or <paramref name="questions"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> is an <see cref="AIContent"/> or a sequence of <see cref="AIContent"/>.</exception>
    /// <remarks>For Native AOT, supply options whose resolver comes from a source-generated context.</remarks>
    public static async Task<DecisionResponse> GetDecisionAsync<TState>(
        this IDecisionClient client,
        TState state,
        JsonSerializerOptions serializerOptions,
        IEnumerable<DecisionQuestion> questions,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(serializerOptions);
        _ = Throw.IfNull(questions);
        ThrowIfContentState(state);

        serializerOptions.MakeReadOnly();
        JsonTypeInfo<TState> stateTypeInfo = (JsonTypeInfo<TState>)serializerOptions.GetTypeInfo(typeof(TState));
        DecisionRequest request = new(questions.ToArray())
        {
            State = JsonSerializer.SerializeToElement(state, stateTypeInfo),
            Contents = contents?.ToArray(),
        };

        return await client.GetDecisionAsync(request, options, cancellationToken).ConfigureAwait(false);
    }

    private static void ThrowIfContentState<TState>(TState state)
    {
        if (state is AIContent or IEnumerable<AIContent>)
        {
            Throw.ArgumentException(
                nameof(state),
                "AIContent can't be used as decision state. Pass it through the contents parameter or DecisionRequest.Contents instead.");
        }
    }
}
