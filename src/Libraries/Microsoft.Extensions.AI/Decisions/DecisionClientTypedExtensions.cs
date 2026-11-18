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
    public static Task<DecisionResponse<TResult>> GetDecisionAsync<TResult>(
        this IDecisionClient client,
        JsonElement state,
        DecisionDefinition<TResult> definition,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(definition);

        return GetDecisionCoreAsync(client, new DecisionRequest(state, definition.Questions, contents?.ToArray()), definition, options, cancellationToken);
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
    public static Task<DecisionResponse<TResult>> GetDecisionAsync<TResult>(
        this IDecisionClient client,
        IEnumerable<AIContent> contents,
        DecisionDefinition<TResult> definition,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(contents);
        _ = Throw.IfNull(definition);

        return GetDecisionCoreAsync(client, new DecisionRequest(null, definition.Questions, contents.ToArray()), definition, options, cancellationToken);
    }

    /// <summary>Evaluates a typed decision definition for application-owned state.</summary>
    /// <typeparam name="TState">The application-owned state type.</typeparam>
    /// <typeparam name="TResult">The application-owned result type.</typeparam>
    /// <param name="client">The decision client.</param>
    /// <param name="state">The state evaluated by every declared question.</param>
    /// <param name="definition">The typed decision definition.</param>
    /// <param name="contents">Optional content evaluated together with <paramref name="state"/>.</param>
    /// <param name="options">Optional request options.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The typed result and the decision response it was bound from.</returns>
    /// <remarks>
    /// <para>
    /// <paramref name="state"/> is serialized using <see cref="DecisionDefinition{TResult}.SerializerOptions"/>, which default to
    /// <see cref="AIJsonUtilities.DefaultOptions"/>. For Native AOT, create the definition with options from a source-generated context
    /// or serialize the state yourself and use the overload that accepts a <see cref="JsonElement"/>.
    /// </para>
    /// <para>
    /// When <paramref name="state"/> is a sequence of <see cref="AIContent"/> and <paramref name="contents"/> is <see langword="null"/>,
    /// the sequence is evaluated as content, as with the overload that accepts only content.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="definition"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> is an <see cref="AIContent"/>, or a sequence of <see cref="AIContent"/> while <paramref name="contents"/> is supplied.</exception>
    /// <exception cref="NotSupportedException">The definition's serializer options do not contain a contract for <typeparamref name="TState"/>.</exception>
    /// <exception cref="DecisionProtocolException">The response cannot be bound to the definition's questions.</exception>
    /// <exception cref="DecisionUnansweredException">A declared question was not answered.</exception>
    public static Task<DecisionResponse<TResult>> GetDecisionAsync<TState, TResult>(
        this IDecisionClient client,
        TState state,
        DecisionDefinition<TResult> definition,
        IEnumerable<AIContent>? contents = null,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(definition);

        // A List<AIContent> or AIContent[] argument binds to this overload rather than to the content-only overload.
        if (state is IEnumerable<AIContent> stateContents && contents is null)
        {
            return GetDecisionAsync(client, stateContents, definition, options, cancellationToken);
        }

        ThrowIfContentState(state);
        return GetDecisionCoreAsync(client, state, definition.GetStateTypeInfo<TState>(), definition, contents, options, cancellationToken);
    }

    internal static Task<DecisionResponse<TResult>> GetDecisionCoreAsync<TState, TResult>(
        IDecisionClient client,
        TState state,
        JsonTypeInfo<TState> stateTypeInfo,
        DecisionDefinition<TResult> definition,
        IEnumerable<AIContent>? contents,
        DecisionOptions? options,
        CancellationToken cancellationToken)
    {
        JsonElement stateJson = JsonSerializer.SerializeToElement(state, stateTypeInfo);
        return GetDecisionCoreAsync(client, new DecisionRequest(stateJson, definition.Questions, contents?.ToArray()), definition, options, cancellationToken);
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

    private static async Task<DecisionResponse<TResult>> GetDecisionCoreAsync<TResult>(
        IDecisionClient client,
        DecisionRequest request,
        DecisionDefinition<TResult> definition,
        DecisionOptions? options,
        CancellationToken cancellationToken)
    {
        DecisionResponse response = await client.GetDecisionAsync(request, options, cancellationToken);
        return definition.Bind(response);
    }
}
