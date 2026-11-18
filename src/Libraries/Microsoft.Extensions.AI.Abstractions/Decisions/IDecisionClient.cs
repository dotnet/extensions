// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Provides provider-neutral decision inference over a heterogeneous question batch.</summary>
/// <remarks>
/// <para>
/// An implementation evaluates every question in <see cref="DecisionRequest.Questions"/> against the same request inputs
/// and returns exactly one <see cref="DecisionAnswer"/> per question, in request order. When the provider does not answer
/// a specific question, the implementation returns an <see cref="UnansweredDecisionAnswer"/> for it rather than failing
/// the whole request or fabricating a value.
/// </para>
/// <para>
/// An implementation must map every <see cref="AIContent"/> item in the request faithfully, or throw
/// <see cref="NotSupportedException"/> for any item it cannot represent. It must never silently drop content.
/// When an item's <see cref="AIContent.RawRepresentation"/> is a provider-native input, implementations should pass it through.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IDecisionClient : IDisposable
{
    /// <summary>Evaluates all questions against the same request inputs.</summary>
    /// <param name="request">The owned decision request.</param>
    /// <param name="options">Optional per-request options.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>A response containing one answer per question, in request order.</returns>
    Task<DecisionResponse> GetResponseAsync(
        DecisionRequest request,
        DecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IDecisionClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the <see cref="IDecisionClient"/>,
    /// including itself, <see cref="DecisionClientMetadata"/>, or any provider-specific services.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}
