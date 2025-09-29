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
/// A delegating chat client that enables groups of tools to be dynamically expanded.
/// </summary>
/// <remarks>
/// <para>
/// On each request, the client initially presents a minimal tool surface consisting of: (a) tools in
/// <see cref="ChatOptions.Tools"/> that are not members of any configured <see cref="AIToolGroup"/>
/// plus (b) a synthetic expansion function. If the model calls the expansion function with a valid group name, the
/// client issues another request with that group's tools visible.
/// Only one group may be expanded per top-level request, and by default at most one expansion loop is performed.
/// </para>
/// <para>
/// This client should typically appear in the pipeline before tool reduction middleware and function invocation
/// middleware. Example order: <c>.UseToolGrouping(...).UseToolReduction(...).UseFunctionInvocation()</c>.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public sealed class ToolGroupingChatClient : DelegatingChatClient
{
    /// <summary>
    /// Gets or sets the name of the parameter used to specify the group name when calling the expansion function.
    /// </summary>
    public const string GroupNameParameter = "groupName";

    private static readonly Delegate _expansionFunctionDelegate = static (string groupName) => string.Empty;

    private readonly ToolGroupingOptions _options;
    private readonly Dictionary<string, AIToolGroup> _toolGroupsByName;
    private readonly HashSet<AITool> _allGroupedTools;
    private readonly AIFunctionDeclaration _expansionFunction;

    /// <summary>Initializes a new instance of the <see cref="ToolGroupingChatClient"/> class.</summary>
    /// <param name="innerClient">Inner client.</param>
    /// <param name="options">Grouping options.</param>
    public ToolGroupingChatClient(IChatClient innerClient, ToolGroupingOptions options)
        : base(innerClient)
    {
        _options = Throw.IfNull(options);
        _toolGroupsByName = options.Groups.ToDictionary(g => g.Name, StringComparer.Ordinal);
#if NET
        _allGroupedTools = options.Groups.SelectMany(g => g.Tools).ToHashSet();
#else
        _allGroupedTools = [];
        foreach (var group in options.Groups)
        {
            foreach (var tool in group.Tools)
            {
                _ = _allGroupedTools.Add(tool);
            }
        }
#endif

        var description = _options.ExpansionFunctionDescription ?? GetDefaultDescription(_options);

        // We create a delegate with signature (string groupName) => string to leverage simple argument binding.
        _expansionFunction = AIFunctionFactory.Create(
            method: _expansionFunctionDelegate,
            name: _options.ExpansionFunctionName,
            description: description)
            .AsDeclarationOnly();

        static string GetDefaultDescription(ToolGroupingOptions options)
        {
            var groupsSummary = string.Join(Environment.NewLine, options.Groups.Select(g => $"- {g.Name}: {g.Description}"));
            return $"Expands a tool group to make its tools available. Call with parameter '{GroupNameParameter}' set to one of: {string.Join(", ", options.Groups.Select(g => g.Name))}.\nGroups:\n{groupsSummary}";
        }
    }

    /// <summary>Gets the default expansion function name.</summary>
    public string ExpansionFunctionName => _options.ExpansionFunctionName;

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_toolGroupsByName.Count == 0)
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
            FunctionInvokingChatClient.FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

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

        if (_toolGroupsByName.Count == 0)
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

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used for the final response
        List<FunctionCallContent>? expansionRequests = null; // expansion requests that need responding to in the current turn
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
        AIToolGroup? expandedGroup = null; // the currently-expanded group of tools
        List<AITool>? modifiedTools = null; // the modified tools list containing the current tool group
        List<ChatResponseUpdate> updates = []; // collected updates from the inner client for the current iteration
        string toolMessageId = Guid.NewGuid().ToString("N"); // stable id for synthetic tool result updates emitted per iteration

        for (int expansionIterationCount = 0; ; expansionIterationCount++)
        {
            // Reset any state accumulated from the prior iteration before calling the inner client again.
            updates.Clear();
            expansionRequests?.Clear();

            await foreach (var update in base.GetStreamingResponseAsync(messages, modifiedOptions, cancellationToken).ConfigureAwait(false))
            {
                if (update is null)
                {
                    Throw.InvalidOperationException("Inner client returned null ChatResponseUpdate.");
                }

                updates.Add(update);

                _ = CopyExpansionRequests(update.Contents, ref expansionRequests);

                yield return update;
            }

            if (expansionRequests is not { Count: > 0 })
            {
                // No expansion function calls were made this iteration, so we're done streaming the response.
                break;
            }

            // Materialize the collected updates into a ChatResponse so the rest of the logic can share code paths
            // with the non-streaming implementation.
            var response = updates.ToChatResponse();
            (responseMessages ??= []).AddRange(response.Messages);

            // Prepare the history for the next iteration.
            FunctionInvokingChatClient.FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            // Add the responses from the group expansions into the augmented history and also into the tracked
            // list of response messages.
            var (addedMessages, shouldTerminate) = ProcessExpansions(expansionRequests!, ref expandedGroup, expansionIterationCount);
            augmentedHistory!.AddRange(addedMessages);
            responseMessages.AddRange(addedMessages);

            // Surface the expansion results to the caller as additional streaming updates.
            foreach (var message in addedMessages)
            {
                yield return FunctionInvokingChatClient.ConvertToolResultMessageToUpdate(message, response.ConversationId, toolMessageId);
            }

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

            if (!_allGroupedTools.Contains(tool))
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
    private (IList<ChatMessage> messagesToAdd, bool shouldTerminate) ProcessExpansions(
        List<FunctionCallContent> expansionRequests,
        ref AIToolGroup? expandedGroup,
        int expansionIterationCount)
    {
        if (expansionRequests.Count == 0)
        {
            return (messagesToAdd: [], shouldTerminate: true);
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
            if (groupName is null || !_toolGroupsByName.TryGetValue(groupName, out var group))
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

        return (messagesToAdd: [new ChatMessage(ChatRole.Tool, contents)], shouldTerminate: !didExpandNewGroup);
    }

    /// <summary>
    /// Copies any function call contents from the provided messages that match the expansion function name
    /// to the <paramref name="expansionRequests"/> list.
    /// </summary>
    /// <param name="messages">The list of chat messages to process.</param>
    /// <param name="expansionRequests">The list of expansion requests to populate.</param>
    /// <returns><see langword="true"/> if any expansion requests were found and copied; otherwise, <see langword="false"/>.</returns>
    private bool CopyExpansionRequests(IList<ChatMessage> messages, [NotNullWhen(true)] ref List<FunctionCallContent>? expansionRequests)
    {
        var any = false;
        foreach (var message in messages)
        {
            any |= CopyExpansionRequests(message.Contents, ref expansionRequests);
        }

        return any;
    }

    /// <summary>
    /// Copies any function call contents from the provided messages that match the expansion function name
    /// to the <paramref name="expansionRequests"/> list.
    /// </summary>
    /// <param name="contents">The list of contents to process.</param>
    /// <param name="expansionRequests">The list of expansion requests to populate.</param>
    /// <returns><see langword="true"/> if any expansion requests were found and copied; otherwise, <see langword="false"/>.</returns>
    private bool CopyExpansionRequests(IList<AIContent> contents, [NotNullWhen(true)] ref List<FunctionCallContent>? expansionRequests)
    {
        var any = false;
        foreach (var content in contents)
        {
            if (content is FunctionCallContent functionCall && string.Equals(functionCall.Name, ExpansionFunctionName, StringComparison.Ordinal))
            {
                (expansionRequests ??= []).Add(functionCall);
                any = true;
            }
        }

        return any;
    }
}
