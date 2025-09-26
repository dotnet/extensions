// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

#pragma warning disable S103 // Lines should not be too long

/// <summary>
/// A delegating chat client that enables a single tool group to be dynamically expanded on demand.
/// </summary>
/// <remarks>
/// <para>
/// On each request the client initially presents a minimal tool surface consisting of: (a) tools in
/// <see cref="ChatOptions.Tools"/> that are not members of any configured <see cref="AIToolGroup"/> (derived "always-on" tools)
/// plus (b) a synthetic expansion function. If the model calls the expansion function with a valid group name, the
/// client issues another request with that group's tools visible (in addition to the always-on tools and expansion tool).
/// Only one group may be expanded per top-level request, and by default at most one expansion loop is performed.
/// </para>
/// <para>
/// This client should typically appear in the pipeline before tool reduction middleware and function invocation
/// middleware. Example order: <c>.UseToolGrouping(...).UseToolReduction(...).UseFunctionInvocation()</c>.
/// </para>
/// <para>
/// Streaming responses are not yet supported for expansion loops; if a stream attempts to trigger expansion an
/// <see cref="NotSupportedException"/> is thrown. A future enhancement can mirror the non-streaming logic.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public sealed class ToolGroupingChatClient : DelegatingChatClient
{
    private const string GroupNameParameter = "groupName";

    private readonly ToolGroupingOptions _options;
    private readonly Dictionary<string, AIToolGroup> _groupMap;
    private readonly HashSet<AITool> _groupedTools;
    private readonly AIFunctionDeclaration _expansionFunction;

    /// <summary>Initializes a new instance of the <see cref="ToolGroupingChatClient"/> class.</summary>
    /// <param name="innerClient">Inner client.</param>
    /// <param name="options">Grouping options.</param>
    public ToolGroupingChatClient(IChatClient innerClient, ToolGroupingOptions options)
        : base(innerClient)
    {
        _options = Throw.IfNull(options);
        _groupMap = options.Groups.ToDictionary(g => g.Name, StringComparer.Ordinal);
        _groupedTools = [];
        foreach (var group in options.Groups)
        {
            foreach (var tool in group.Tools)
            {
                _ = _groupedTools.Add(tool);
            }
        }

        // TO-DO: Allow customization of these options.
        var groupsSummary = string.Join(Environment.NewLine, _groupMap.Values.Select(g => $"- {g.Name}: {g.Description}"));
        var description = $"Expands a tool group to make its tools available. Call with parameter '{GroupNameParameter}' set to one of: {string.Join(", ", _groupMap.Keys)}.\nGroups:\n{groupsSummary}";

        // We create a delegate with signature (string groupName) => string to leverage simple argument binding.
        _expansionFunction = AIFunctionFactory.Create(
            method: static (string groupName) => string.Empty,
            name: _options.ExpansionFunctionName,
            description: description)
            .AsDeclarationOnly();
    }

    /// <summary>Gets the default expansion function name.</summary>
    public string ExpansionFunctionName => _options.ExpansionFunctionName;

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_groupMap.Count == 0)
        {
            // If there are no specified tool groups, then tool expansion isn't possible.
            // We'll just call directly through to the inner chat client.
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }

        // Copy the original messages in order to avoid enumerating the original messages multiple times.
        // The IEnumerable can represent an arbitrary amount of work.
        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        List<AITool> baseTools = ComputeBaseTools(options);
        ChatOptions modifiedOptions = options?.Clone() ?? new();
        modifiedOptions.Tools = baseTools;

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        ChatResponse? response = null; // the response from the inner client, which is possibly modified and then eventually returned
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used for the final response
        List<FunctionCallContent>? expansionRequests = null; // expansion requests that need responding to in the current turn
        UsageDetails? totalUsage = null; // tracked usage across all turns, to be used for the final response
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
        AIToolGroup? expandedGroup = null; // the currently-expanded group of tools
        List<AITool>? modifiedTools = null; // the modified tools list containing the current tool group

        for (var expansionIterationCount = 0; ; expansionIterationCount++)
        {
            expansionRequests?.Clear();

            // Make the call to the inner client.
            response = await base.GetResponseAsync(messages, modifiedOptions, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                Throw.InvalidOperationException("Inner client returned null ChatResponse.");
            }

            // Any expansions to perform? If yes, ensure we're tracking that work in expansionRequests.
            bool requiresExpansion = CopyExpansionRequests(response.Messages, ref expansionRequests);
            if (!requiresExpansion && expansionIterationCount == 0)
            {
                // In a common case where we make an initial request and there's no function calling work required,
                // fast path out by just returning the original response.
                return response;
            }

            // Track aggregate details from the response, including all of the response messages and usage details.
            (responseMessages ??= []).AddRange(response.Messages);
            if (response.Usage is not null)
            {
                if (totalUsage is not null)
                {
                    totalUsage.Add(response.Usage);
                }
                else
                {
                    totalUsage = response.Usage;
                }
            }

            // Prepare the history for the next iteration.
            FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            // Add the responses from the group expansions into the augmented history and also into the tracked
            // list of response messages.
            var (addedMessages, shouldTerminate) = ProcessExpansions(expansionRequests!, ref expandedGroup, expansionIterationCount);
            augmentedHistory.AddRange(addedMessages);
            responseMessages.AddRange(addedMessages);

            if (shouldTerminate)
            {
                // No more expansions to handle.
                break;
            }

            // If a valid group was requested for expansion, and it does not match the currently-expanded group,
            // update the tools list to contain the newly-expanded tool group.
            (modifiedTools ??= []).Clear();
            modifiedTools.AddRange(baseTools);
            modifiedTools.AddRange(expandedGroup!.Tools);
            modifiedOptions.Tools = modifiedTools;
            modifiedOptions.ConversationId = response.ConversationId;
        }

        Debug.Assert(responseMessages is not null, "Expected to only be here if we have response messages.");
        response.Messages = responseMessages!;
        response.Usage = totalUsage;

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_groupMap.Count == 0)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }

            yield break;
        }

        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        List<AITool> baseTools = ComputeBaseTools(options);
        ChatOptions modifiedOptions = options?.Clone() ?? new();
        modifiedOptions.Tools = baseTools;

        List<ChatMessage>? augmentedHistory = null;
        List<ChatMessage>? responseMessages = null;
        List<FunctionCallContent>? expansionRequests = null;
        bool lastIterationHadConversationId = false;
        AIToolGroup? expandedGroup = null;
        List<AITool>? modifiedTools = null;
        List<ChatResponseUpdate> updates = [];
        string toolMessageId = Guid.NewGuid().ToString("N");

        for (int expansionIterationCount = 0; ; expansionIterationCount++)
        {
            updates.Clear();
            expansionRequests?.Clear();

            await foreach (var update in base.GetStreamingResponseAsync(messages, modifiedOptions, cancellationToken).ConfigureAwait(false))
            {
                if (update is null)
                {
                    Throw.InvalidOperationException("Inner client returned null ChatResponseUpdate.");
                }

                updates.Add(update);

                if (update.Contents is { Count: > 0 })
                {
                    for (int i = 0; i < update.Contents.Count; i++)
                    {
                        if (update.Contents[i] is FunctionCallContent functionCall &&
                            string.Equals(functionCall.Name, ExpansionFunctionName, StringComparison.Ordinal))
                        {
                            (expansionRequests ??= []).Add(functionCall);
                        }
                    }
                }

                yield return update;
            }

            if (expansionRequests is not { Count: > 0 })
            {
                break;
            }

            var response = updates.ToChatResponse();
            (responseMessages ??= []).AddRange(response.Messages);

            FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            var (addedMessages, shouldTerminate) = ProcessExpansions(expansionRequests!, ref expandedGroup, expansionIterationCount);
            Debug.Assert(augmentedHistory is not null, "Augmented history should have been initialized.");
            augmentedHistory!.AddRange(addedMessages);
            responseMessages.AddRange(addedMessages);

            foreach (var message in addedMessages)
            {
                message.MessageId ??= toolMessageId;
                yield return ConvertToolResultMessageToUpdate(message, response.ConversationId, message.MessageId);
            }

            if (shouldTerminate)
            {
                break;
            }

            (modifiedTools ??= []).Clear();
            modifiedTools.AddRange(baseTools);
            modifiedTools.AddRange(expandedGroup!.Tools);
            modifiedOptions.Tools = modifiedTools;
            modifiedOptions.ConversationId = response.ConversationId;
        }
    }

    private static ChatResponseUpdate ConvertToolResultMessageToUpdate(ChatMessage message, string? conversationId, string? messageId) =>
        new()
        {
            AdditionalProperties = message.AdditionalProperties,
            AuthorName = message.AuthorName,
            ConversationId = conversationId,
            CreatedAt = DateTimeOffset.UtcNow,
            Contents = message.Contents,
            RawRepresentation = message.RawRepresentation,
            ResponseId = messageId,
            MessageId = messageId,
            Role = message.Role,
        };

    /// <summary>Prepares the various chat message lists after a response from the inner client and before invoking functions.</summary>
    /// <param name="originalMessages">The original messages provided by the caller.</param>
    /// <param name="messages">The messages reference passed to the inner client.</param>
    /// <param name="augmentedHistory">The augmented history containing all the messages to be sent.</param>
    /// <param name="response">The most recent response being handled.</param>
    /// <param name="allTurnsResponseMessages">A list of all response messages received up until this point.</param>
    /// <param name="lastIterationHadConversationId">Whether the previous iteration's response had a conversation ID.</param>
    private static void FixupHistories(
        IEnumerable<ChatMessage> originalMessages,
        ref IEnumerable<ChatMessage> messages,
        [NotNull] ref List<ChatMessage>? augmentedHistory,
        ChatResponse response,
        List<ChatMessage> allTurnsResponseMessages,
        ref bool lastIterationHadConversationId)
    {
        // We're now going to need to augment the history with function result contents.
        // That means we need a separate list to store the augmented history.
        if (response.ConversationId is not null)
        {
            // The response indicates the inner client is tracking the history, so we don't want to send
            // anything we've already sent or received.
            if (augmentedHistory is not null)
            {
                augmentedHistory.Clear();
            }
            else
            {
                augmentedHistory = [];
            }

            lastIterationHadConversationId = true;
        }
        else if (lastIterationHadConversationId)
        {
            // In the very rare case where the inner client returned a response with a conversation ID but then
            // returned a subsequent response without one, we want to reconstitute the full history. To do that,
            // we can populate the history with the original chat messages and then all of the response
            // messages up until this point, which includes the most recent ones.
            augmentedHistory ??= [];
            augmentedHistory.Clear();
            augmentedHistory.AddRange(originalMessages);
            augmentedHistory.AddRange(allTurnsResponseMessages);

            lastIterationHadConversationId = false;
        }
        else
        {
            // If augmentedHistory is already non-null, then we've already populated it with everything up
            // until this point (except for the most recent response). If it's null, we need to seed it with
            // the chat history provided by the caller.
            augmentedHistory ??= originalMessages.ToList();

            // Now add the most recent response messages.
            augmentedHistory.AddMessages(response);

            lastIterationHadConversationId = false;
        }

        // Use the augmented history as the new set of messages to send.
        messages = augmentedHistory;
    }

    /// <summary>
    /// Generates a list of base AI tools by combining the default expansion function with additional tools specified in
    /// the provided chat options, excluding any tools that are grouped.
    /// </summary>
    /// <param name="options">The chat options containing the set of tools to include. If null, only the default expansion function is used.</param>
    /// <returns>A list of AI tools consisting of the default expansion function and any additional tools from the chat options that are not grouped.</returns>
    private List<AITool> ComputeBaseTools(ChatOptions? options)
    {
        List<AITool> baseTools = [_expansionFunction];

        foreach (var tool in options?.Tools ?? [])
        {
            if (string.Equals(tool.Name, _expansionFunction.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The group expansion tool with name '{_expansionFunction.Name}' collides with a registered tool of the same name.");
            }

            if (!_groupedTools.Contains(tool))
            {
                baseTools.Add(tool);
            }
        }

        return baseTools;
    }

    /// <summary>
    /// Processes a set of function call expansion requests and determines which chat messages to add and whether
    /// expansion should terminate.
    /// </summary>
    /// <remarks>
    /// Expansion requests that exceed the maximum allowed iterations or specify invalid or duplicate
    /// groups are ignored. Only one new group can be expanded per call; subsequent requests for the same group are
    /// treated as duplicates. The expanded group reference is updated only if a new group is successfully
    /// expanded.
    /// </remarks>
    /// <param name="expansionRequests">A list of expansion requests to process. Each request specifies a function call and its associated arguments.</param>
    /// <param name="expandedGroup">A reference to the tool group that has been expanded. This parameter is updated if a new group is expanded during processing.</param>
    /// <param name="expansionIterationCount">The current number of expansion iterations performed for this request. Used to enforce the maximum allowed expansions per request.</param>
    /// <returns>
    /// A tuple containing a list of chat messages to add in response to the expansion requests, and a boolean value
    /// indicating whether expansion should terminate. If no valid expansions are processed, the message list will be
    /// empty and termination will be signaled.
    /// </returns>
    private (IList<ChatMessage> MessagesToAdd, bool ShouldTerminate) ProcessExpansions(
        List<FunctionCallContent> expansionRequests,
        ref AIToolGroup? expandedGroup,
        int expansionIterationCount)
    {
        if (expansionRequests.Count == 0)
        {
            return (MessagesToAdd: [], ShouldTerminate: true);
        }

        var didExpandNewGroup = false;
        var maxExpansionsReached = expansionIterationCount >= _options.MaxExpansionsPerRequest;
        var contents = new List<AIContent>(expansionRequests.Count);

        foreach (var expansionRequest in expansionRequests)
        {
            if (maxExpansionsReached)
            {
                contents.Add(new FunctionResultContent(
                    callId: expansionRequest.CallId,
                    result: "Max expansion iteration count reached; ignoring additional expansion request."));
                continue;
            }

            if (expansionRequest.Arguments is not { Count: > 0 } arguments ||
                !arguments.TryGetValue(GroupNameParameter, out var groupNameArg) ||
                groupNameArg is null)
            {
                contents.Add(new FunctionResultContent(
                    callId: expansionRequest.CallId,
                    result: "No group name was specified; ignoring expansion request."));
                continue;
            }

            var groupName = groupNameArg.ToString();
            if (groupName is null || !_groupMap.TryGetValue(groupName, out var group))
            {
                contents.Add(new FunctionResultContent(
                    callId: expansionRequest.CallId,
                    result: $"The specific group name '{groupName}' was invalid; ignoring expansion request."));
                continue;
            }

            if (group == expandedGroup)
            {
                contents.Add(new FunctionResultContent(
                    callId: expansionRequest.CallId,
                    result: $"Ignoring duplicate expansion of group '{groupName}'."));
                continue;
            }

            didExpandNewGroup = true;
            expandedGroup = group;
            var groupsSummary = string.Join(Environment.NewLine, group.Tools.Select(t => $"- {t.Name}: {t.Description}"));
            contents.Add(new FunctionResultContent(
                callId: expansionRequest.CallId,
                result: $"Successfully expanded group '{groupName}'. Only this group's tools are now available:{Environment.NewLine}{groupsSummary}"));
        }

        return (MessagesToAdd: [new ChatMessage(ChatRole.Tool, contents)], ShouldTerminate: !didExpandNewGroup);
    }

    private bool CopyExpansionRequests(IList<ChatMessage> messages, [NotNullWhen(true)] ref List<FunctionCallContent>? expansionRequests)
    {
        var any = false;
        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent functionCall && string.Equals(functionCall.Name, ExpansionFunctionName, StringComparison.Ordinal))
                {
                    (expansionRequests ??= []).Add(functionCall);
                    any = true;
                }
            }
        }

        return any;
    }
}
