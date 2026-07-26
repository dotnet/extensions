// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

public partial class MockChatClient
{
    /// <summary>Seeds text responses from a dictionary for matching requests.</summary>
    /// <param name="responses">The response text keyed by request match values.</param>
    /// <param name="requestPredicate">
    /// An optional predicate used to select whether a response key applies to a request. By default, this uses
    /// <c>string.Equals(request.LastUserText, key, StringComparison.OrdinalIgnoreCase)</c>.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove every response after its first matching request; otherwise the
    /// responses remain reusable.
    /// </param>
    /// <param name="getResponse">
    /// An optional asynchronous function applied to each selected response. The function receives the response and the
    /// cancellation token from the matching chat-client call.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <remarks>
    /// Dictionary entries are seeded in enumeration order. When multiple entries match the same request, the last
    /// entry wins. Add more specific matches last.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="responses"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddResponses(
        Dictionary<string, string> responses,
        Func<MockChatClientRequest, string, bool>? requestPredicate = null,
        bool singleUse = false,
        Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse = null) =>
        AddResponsesFromDictionary(responses, requestPredicate, singleUse, getResponse);

    /// <summary>Seeds text responses from an enumerable collection for matching requests.</summary>
    /// <param name="responses">The response text keyed by request match values.</param>
    /// <param name="requestPredicate">
    /// An optional predicate used to select whether a response key applies to a request. By default, this uses
    /// <c>string.Equals(request.LastUserText, key, StringComparison.OrdinalIgnoreCase)</c>.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove every response after its first matching request; otherwise the
    /// responses remain reusable.
    /// </param>
    /// <param name="getResponse">
    /// An optional asynchronous function applied to each selected response. The function receives the response and the
    /// cancellation token from the matching chat-client call.
    /// </param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    /// <remarks>
    /// Entries are seeded in enumeration order. When multiple entries match the same request, the last entry wins.
    /// Unlike dictionaries, enumerable collections can contain repeated keys.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="responses"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public virtual MockChatClient AddResponses(
        IEnumerable<KeyValuePair<string, string>> responses,
        Func<MockChatClientRequest, string, bool>? requestPredicate = null,
        bool singleUse = false,
        Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse = null) =>
        AddResponsesFromEnumerable(responses, requestPredicate, singleUse, getResponse);

    /// <summary>Adds text response seeds from a dictionary.</summary>
    /// <param name="responses">The response text keyed by request match values.</param>
    /// <param name="requestPredicate">Predicate used to select whether a response key applies to a request.</param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove every response after its first matching request; otherwise the
    /// responses remain reusable.
    /// </param>
    /// <param name="getResponse">An asynchronous function applied to each selected response.</param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    protected virtual MockChatClient AddResponsesFromDictionary(
        Dictionary<string, string> responses,
        Func<MockChatClientRequest, string, bool>? requestPredicate,
        bool singleUse,
        Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse)
        => AddResponsesFromEnumerable(responses, requestPredicate, singleUse, getResponse);

    /// <summary>Adds text response seeds from an enumerable collection.</summary>
    /// <param name="responses">The response text keyed by request match values.</param>
    /// <param name="requestPredicate">Predicate used to select whether a response key applies to a request.</param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove every response after its first matching request; otherwise the
    /// responses remain reusable.
    /// </param>
    /// <param name="getResponse">An asynchronous function applied to each selected response.</param>
    /// <returns>The same <see cref="MockChatClient"/> instance for chaining.</returns>
    protected virtual MockChatClient AddResponsesFromEnumerable(
        IEnumerable<KeyValuePair<string, string>> responses,
        Func<MockChatClientRequest, string, bool>? requestPredicate,
        bool singleUse,
        Func<ChatResponse, CancellationToken, Task<ChatResponse>>? getResponse)
    {
        _ = Throw.IfNull(responses);
        ThrowIfDisposed();

        requestPredicate ??= static (request, key) =>
            string.Equals(request.LastUserText, key, StringComparison.OrdinalIgnoreCase);
        getResponse ??= static (response, _) => Task.FromResult(response);

        foreach (KeyValuePair<string, string> response in responses)
        {
            string key = Throw.IfNull(response.Key);
            string value = Throw.IfNull(response.Value);

            _ = AddResponse(
                request => requestPredicate(request, key),
                (_, cancellationToken) => getResponse(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, value)),
                    cancellationToken),
                singleUse);
        }

        return this;
    }
}
