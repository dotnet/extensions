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

/// <summary>Provides extension methods for evaluating a <see cref="DecisionDefinition{TResult}"/> with an <see cref="IDecisionClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DecisionClientTypedExtensions
{
    /// <summary>Evaluates a typed decision definition for a JSON state.</summary>
    /// <typeparam name="TResult">The application-owned result type.</typeparam>
    /// <param name="client">The decision client.</param>
    /// <param name="state">The JSON state evaluated by every declared question.</param>
    /// <param name="definition">The typed decision definition.</param>
    /// <param name="contents">Optional content evaluated together with <paramref name="state"/>.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The typed result and the decision response it was bound from.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="definition"/> is <see langword="null"/>.</exception>
    /// <exception cref="DecisionProtocolException">The response cannot be bound to the definition's questions.</exception>
    /// <exception cref="DecisionUnansweredException">A declared question was not answered.</exception>
    public static Task<DecisionResponse<TResult>> GetResponseAsync<TResult>(
        this IDecisionClient client,
        JsonElement state,
        DecisionDefinition<TResult> definition,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(definition);

        return GetResponseCoreAsync(client, new DecisionRequest(state, definition.Questions, contents?.ToArray()), definition, options, cancellationToken);
    }

    /// <summary>Evaluates a typed decision definition for content.</summary>
    /// <typeparam name="TResult">The application-owned result type.</typeparam>
    /// <param name="client">The decision client.</param>
    /// <param name="contents">The content evaluated by every declared question.</param>
    /// <param name="definition">The typed decision definition.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The typed result and the decision response it was bound from.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="client"/>, <paramref name="contents"/>, or <paramref name="definition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="contents"/> is empty.</exception>
    /// <exception cref="DecisionProtocolException">The response cannot be bound to the definition's questions.</exception>
    /// <exception cref="DecisionUnansweredException">A declared question was not answered.</exception>
    public static Task<DecisionResponse<TResult>> GetResponseAsync<TResult>(
        this IDecisionClient client,
        IEnumerable<AIContent> contents,
        DecisionDefinition<TResult> definition,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(contents);
        _ = Throw.IfNull(definition);

        return GetResponseCoreAsync(client, new DecisionRequest(null, definition.Questions, contents.ToArray()), definition, options, cancellationToken);
    }

    /// <summary>Evaluates a typed decision definition for application-owned state.</summary>
    /// <typeparam name="TState">The application-owned state type.</typeparam>
    /// <typeparam name="TResult">The application-owned result type.</typeparam>
    /// <param name="client">The decision client.</param>
    /// <param name="state">The state evaluated by every declared question.</param>
    /// <param name="stateTypeInfo">The JSON contract for <typeparamref name="TState"/>.</param>
    /// <param name="definition">The typed decision definition.</param>
    /// <param name="contents">Optional content evaluated together with <paramref name="state"/>.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The typed result and the decision response it was bound from.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="client"/>, <paramref name="stateTypeInfo"/>, or <paramref name="definition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DecisionProtocolException">The response cannot be bound to the definition's questions.</exception>
    /// <exception cref="DecisionUnansweredException">A declared question was not answered.</exception>
    public static Task<DecisionResponse<TResult>> GetResponseAsync<TState, TResult>(
        this IDecisionClient client,
        TState state,
        JsonTypeInfo<TState> stateTypeInfo,
        DecisionDefinition<TResult> definition,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(stateTypeInfo);
        _ = Throw.IfNull(definition);

        JsonElement stateJson = JsonSerializer.SerializeToElement(state, stateTypeInfo);
        return GetResponseCoreAsync(client, new DecisionRequest(stateJson, definition.Questions, contents?.ToArray()), definition, options, cancellationToken);
    }

    private static async Task<DecisionResponse<TResult>> GetResponseCoreAsync<TResult>(
        IDecisionClient client,
        DecisionRequest request,
        DecisionDefinition<TResult> definition,
        DecisionOptions? options,
        CancellationToken cancellationToken)
    {
        DecisionResponse response = await client.GetResponseAsync(request, options, cancellationToken);
        return definition.Bind(response);
    }
}
