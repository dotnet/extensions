// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Decision task contracts are co-located as one API surface.
#pragma warning disable SA1649 // Decision task contracts are co-located as one API surface.
#pragma warning disable SA1204 // Static extension members are intentionally grouped after the task contracts.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes a host-defined decision task that can be exposed as one bounded AI function invocation.
/// </summary>
/// <typeparam name="TState">The application-owned state type supplied to the decision provider.</typeparam>
/// <typeparam name="TResult">The application-owned result type produced by the local mapper.</typeparam>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionTask<TState, TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionTask{TState, TResult}"/> class.
    /// </summary>
    /// <param name="questions">The nonempty, ordered questions evaluated by the task.</param>
    /// <param name="stateTypeInfo">The source-generated JSON contract for <typeparamref name="TState"/>.</param>
    /// <param name="resultBinding">The explicit mapper for the complete decision response.</param>
    /// <param name="resultTypeInfo">
    /// The source-generated JSON contract for the safe function result, including
    /// <typeparamref name="TResult"/>.
    /// </param>
    /// <param name="featureSchema">
    /// The named feature schema used to retain a portable, probability-rich projection.
    /// Every coordinate must reference a question in <paramref name="questions"/>.
    /// </param>
    public DecisionTask(
        IEnumerable<DecisionQuestion> questions,
        JsonTypeInfo<TState> stateTypeInfo,
        DecisionResultBinding<TResult> resultBinding,
        JsonTypeInfo<DecisionFunctionResult<TResult>> resultTypeInfo,
        DecisionFeatureSchema featureSchema)
    {
        _ = Throw.IfNull(questions);
        StateTypeInfo = Throw.IfNull(stateTypeInfo);
        ResultBinding = Throw.IfNull(resultBinding);
        ResultTypeInfo = Throw.IfNull(resultTypeInfo);
        FeatureSchema = Throw.IfNull(featureSchema);

        DecisionQuestion[] questionSnapshot = questions.ToArray();
        if (questionSnapshot.Length == 0 || Array.Exists(questionSnapshot, static question => question is null))
        {
            Throw.ArgumentException(nameof(questions), "Questions must be nonempty.");
        }

        HashSet<string> questionIds = new(StringComparer.Ordinal);
        foreach (DecisionQuestion question in questionSnapshot)
        {
            if (!questionIds.Add(question.Id))
            {
                Throw.ArgumentException(nameof(questions), "Question IDs must be unique using ordinal comparison.");
            }
        }

        foreach (DecisionFeatureCoordinate coordinate in featureSchema.Coordinates)
        {
            if (!questionIds.Contains(coordinate.QuestionId))
            {
                Throw.ArgumentException(
                    nameof(featureSchema),
                    $"Feature coordinate '{coordinate.Name}' references unknown question '{coordinate.QuestionId}'.");
            }
        }

        Questions = Array.AsReadOnly(questionSnapshot);
    }

    /// <summary>Gets the owned question snapshot.</summary>
    public IReadOnlyList<DecisionQuestion> Questions { get; }

    /// <summary>Gets the explicit source-generated JSON contract for the task state.</summary>
    public JsonTypeInfo<TState> StateTypeInfo { get; }

    /// <summary>Gets the explicit mapper for the complete response.</summary>
    public DecisionResultBinding<TResult> ResultBinding { get; }

    /// <summary>Gets the explicit source-generated JSON contract for the function result.</summary>
    public JsonTypeInfo<DecisionFunctionResult<TResult>> ResultTypeInfo { get; }

    /// <summary>Gets the named feature schema projected into the function result.</summary>
    public DecisionFeatureSchema FeatureSchema { get; }
}

/// <summary>
/// Represents the safe result of a decision function invocation.
/// </summary>
/// <typeparam name="TResult">The application-owned result type produced by the local mapper.</typeparam>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionFunctionResult<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionFunctionResult{TResult}"/> class.
    /// </summary>
    /// <param name="result">The application-owned mapped result.</param>
    /// <param name="features">The portable named feature projection.</param>
    /// <param name="provenance">The decision provider/model provenance, when reported.</param>
    /// <param name="usage">The decision usage, kept separate from chat usage.</param>
    public DecisionFunctionResult(
        TResult result,
        DecisionFeatureVector features,
        DecisionProvenance? provenance = null,
        UsageDetails? usage = null)
    {
        Result = result;
        Features = Throw.IfNull(features);
        Provenance = provenance;
        Usage = usage;
    }

    /// <summary>Gets the application-owned mapped result.</summary>
    public TResult Result { get; }

    /// <summary>Gets the portable named feature projection.</summary>
    public DecisionFeatureVector Features { get; }

    /// <summary>Gets decision provider/model provenance, when reported.</summary>
    public DecisionProvenance? Provenance { get; }

    /// <summary>Gets decision usage, kept separate from chat usage.</summary>
    public UsageDetails? Usage { get; }
}

/// <summary>Provides interoperability helpers for decision tasks.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DecisionFunctionExtensions
{
    /// <summary>
    /// Creates one bounded <see cref="AIFunction"/> that evaluates the supplied decision task.
    /// </summary>
    /// <typeparam name="TState">The application-owned state type accepted by the function.</typeparam>
    /// <typeparam name="TResult">The application-owned mapped result type.</typeparam>
    /// <param name="task">The explicit decision task definition.</param>
    /// <param name="client">The caller-owned decision client to invoke.</param>
    /// <param name="decisionOptions">Optional request options captured for each invocation.</param>
    /// <param name="functionOptions">Optional AIFunction metadata and source-generated serializer options.</param>
    /// <returns>An invocable function whose only input is <typeparamref name="TState"/>.</returns>
    /// <remarks>
    /// <para>
    /// The returned function closes over the questions, feature schema, mapper, and client. It performs exactly one
    /// decision request per invocation, propagates provider, mapper, and cancellation failures, and never disposes
    /// the caller-owned client.
    /// </para>
    /// <para>
    /// The default result contains only the explicitly mapped result, named native-double features, provenance, and
    /// usage. Raw state, raw provider objects, reasoning text, and provider extension dictionaries are not included.
    /// The supplied <see cref="AIFunctionFactoryOptions.SerializerOptions"/> must contain source-generated metadata
    /// for <typeparamref name="TState"/> and <see cref="DecisionFunctionResult{TResult}"/> when reflection-based JSON
    /// serialization is disabled.
    /// </para>
    /// </remarks>
    public static AIFunction AsAIFunction<TState, TResult>(
        this DecisionTask<TState, TResult> task,
        IDecisionClient client,
        DecisionOptions? decisionOptions = null,
        AIFunctionFactoryOptions? functionOptions = null)
    {
        _ = Throw.IfNull(task);
        _ = Throw.IfNull(client);

        DecisionOptions? optionsSnapshot = decisionOptions?.Clone();
        AIFunctionFactoryOptions effectiveOptions = new()
        {
            Name = functionOptions?.Name ?? "decision",
            Description = functionOptions?.Description,
            SerializerOptions = functionOptions?.SerializerOptions ?? task.ResultTypeInfo.Options,
            JsonSchemaCreateOptions = functionOptions?.JsonSchemaCreateOptions,
            AdditionalProperties = functionOptions?.AdditionalProperties,
            ConfigureParameterBinding = functionOptions?.ConfigureParameterBinding,
            MarshalResult = functionOptions?.MarshalResult,
            ExcludeResultSchema = functionOptions?.ExcludeResultSchema ?? false,
        };

        Func<TState, CancellationToken, Task<DecisionFunctionResult<TResult>>> invoke = InvokeAsync;
        return AIFunctionFactory.Create(invoke, effectiveOptions);

        async Task<DecisionFunctionResult<TResult>> InvokeAsync(
            TState state,
            CancellationToken cancellationToken)
        {
            DecisionResponse response = await client.GetResponseAsync(
                state,
                task.StateTypeInfo,
                task.Questions,
                optionsSnapshot,
                cancellationToken).ConfigureAwait(false);

            TResult result = response.Bind(task.ResultBinding);
            DecisionFeatureVector features = DecisionFeatureProjection.Project(response, task.FeatureSchema);
            return new(result, features, response.Provenance, response.Usage);
        }
    }
}
