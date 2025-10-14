// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable IDE0058 // Expression value is never used

/// <summary>
/// A chat client that enables tool groups (see <see cref="AIToolGroup"/>) to be dynamically expanded.
/// </summary>
/// <remarks>
/// <para>
/// On each request, this chat client initially presents a minimal tool surface consisting of: (a) a function
/// returning the current list of available groups plus (b) a synthetic expansion function plus (c) tools in
/// <see cref="ChatOptions.Tools"/> that are not <see cref="AIToolGroup"/> instances.
/// If the model calls the expansion function with a valid group name, the
/// client issues another request with that group's tools visible.
/// Only one group may be expanded per top-level request, and by default at most three expansion loops are performed.
/// </para>
/// <para>
/// This client should typically appear in the pipeline before tool reduction middleware and function invocation
/// middleware. Example order: <c>.UseToolGrouping(...).UseToolReduction(...).UseFunctionInvocation()</c>.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public sealed class ToolGroupingChatClient : DelegatingChatClient
{
    private const string ExpansionFunctionGroupNameParameter = "groupName";
    private static readonly Delegate _expansionFunctionDelegate = static string (string groupName)
        => throw new InvalidOperationException("The tool expansion function should not be invoked directly.");

    private readonly int _maxExpansionsPerRequest;
    private readonly AIFunctionDeclaration _expansionFunction;
    private readonly string _listGroupsFunctionName;
    private readonly string _listGroupsFunctionDescription;

    /// <summary>Initializes a new instance of the <see cref="ToolGroupingChatClient"/> class.</summary>
    /// <param name="innerClient">Inner client.</param>
    /// <param name="options">Grouping options.</param>
    public ToolGroupingChatClient(IChatClient innerClient, ToolGroupingOptions options)
        : base(innerClient)
    {
        _ = Throw.IfNull(options);

        _maxExpansionsPerRequest = options.MaxExpansionsPerRequest;
        _listGroupsFunctionName = options.ListGroupsFunctionName;
        _listGroupsFunctionDescription = options.ListGroupsFunctionDescription
            ?? "Returns the list of available tool groups that can be expanded.";

        var expansionFunctionName = options.ExpansionFunctionName;
        var expansionDescription = options.ExpansionFunctionDescription
            ?? $"Expands a tool group to make its tools available. Use the '{_listGroupsFunctionName}' function to see available groups.";

        _expansionFunction = AIFunctionFactory.Create(
            method: _expansionFunctionDelegate,
            name: expansionFunctionName,
            description: expansionDescription).AsDeclarationOnly();
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var toolGroups = ExtractToolGroups(options);
        if (toolGroups is not { Count: > 0 })
        {
            // If there are no tool groups, then tool expansion isn't possible.
            // We'll just call directly through to the inner chat client.
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }

        // Copy the original messages in order to avoid enumerating the original messages multiple times.
        // The IEnumerable can represent an arbitrary amount of work.
        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        // Build top-level groups dictionary
        var topLevelToolGroupsByName = toolGroups.ToDictionary(g => g.Name, StringComparer.Ordinal);

        // Track the currently-expanded group and all its constituent tools
        AIToolGroup? expandedGroup = null;
        List<AIToolGroup>? expandedGroupToolGroups = null; // tool groups within the currently-expanded tool group
        List<AITool>? expandedGroupTools = null; // non-group tools within the currently-expanded tool group

        // Create the "list groups" function. Its behavior is controlled by values captured in the lambda below.
        var listGroupsFunction = AIFunctionFactory.Create(
            method: () => CreateListGroupsResult(expandedGroup, toolGroups, expandedGroupToolGroups),
            name: _listGroupsFunctionName,
            description: _listGroupsFunctionDescription);

        // Construct new chat options containing ungrouped tools and utility functions.
        List<AITool> baseTools = ComputeBaseTools(options, listGroupsFunction);
        ChatOptions modifiedOptions = options?.Clone() ?? new();
        modifiedOptions.Tools = baseTools;

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        ChatResponse? response = null; // the response from the inner client, which is possibly modified and then eventually returned
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used for the final response
        List<FunctionCallContent>? expansionRequests = null; // expansion requests that need responding to in the current turn
        UsageDetails? totalUsage = null; // tracked usage across all turns, to be used for the final response
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
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
            bool requiresExpansion =
                expansionIterationCount < _maxExpansionsPerRequest &&
                CopyExpansionRequests(response.Messages, ref expansionRequests);

            if (!requiresExpansion && expansionIterationCount == 0)
            {
                // Fast path: no function calling work required
                return response;
            }

            // Track aggregate details from the response
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

            if (!requiresExpansion)
            {
                // No more work to do.
                break;
            }

            // Prepare the history for the next iteration.
            FunctionInvokingChatClient.FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            expandedGroupTools ??= [];
            expandedGroupToolGroups ??= [];
            (var addedMessages, expandedGroup) = await ProcessExpansionsAsync(
                expansionRequests!,
                topLevelToolGroupsByName,
                expandedGroupTools,
                expandedGroupToolGroups,
                expandedGroup,
                cancellationToken);

            augmentedHistory.AddRange(addedMessages);
            responseMessages.AddRange(addedMessages);

            (modifiedTools ??= []).Clear();
            modifiedTools.AddRange(baseTools);
            modifiedTools.AddRange(expandedGroupTools);
            modifiedOptions.Tools = modifiedTools;
            modifiedOptions.ConversationId = response.ConversationId;
        }

        Debug.Assert(responseMessages is not null, "Expected to only be here if we have response messages.");
        response.Messages = responseMessages!;
        response.Usage = totalUsage;

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var toolGroups = ExtractToolGroups(options);
        if (toolGroups is not { Count: > 0 })
        {
            // No tool groups, just call through
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }

            yield break;
        }

        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        // Build top-level groups dictionary
        var topLevelToolGroupsByName = toolGroups.ToDictionary(g => g.Name, StringComparer.Ordinal);

        // Track the currently-expanded group and all its constituent tools
        AIToolGroup? expandedGroup = null;
        List<AIToolGroup>? expandedGroupToolGroups = null; // tool groups within the currently-expanded tool group
        List<AITool>? expandedGroupTools = null; // non-group tools within the currently-expanded tool group

        // Create the "list groups" function. Its behavior is controlled by values captured in the lambda below.
        var listGroupsFunction = AIFunctionFactory.Create(
            method: () => CreateListGroupsResult(expandedGroup, toolGroups, expandedGroupToolGroups),
            name: _listGroupsFunctionName,
            description: _listGroupsFunctionDescription);

        // Construct new chat options containing ungrouped tools and utility functions.
        List<AITool> baseTools = ComputeBaseTools(options, listGroupsFunction);
        ChatOptions modifiedOptions = options?.Clone() ?? new();
        modifiedOptions.Tools = baseTools;

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used for the final response
        List<FunctionCallContent>? expansionRequests = null; // expansion requests that need responding to in the current turn
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
        List<ChatResponseUpdate> updates = []; // collected updates from the inner client for the current iteration
        List<AITool>? modifiedTools = null;
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

            if (expansionIterationCount >= _maxExpansionsPerRequest || expansionRequests is not { Count: > 0 })
            {
                // We've either hit the expansion iteration limit or no expansion function calls were made,
                // so we're done streaming the response.
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
            expandedGroupTools ??= [];
            expandedGroupToolGroups ??= [];
            (var addedMessages, expandedGroup) = await ProcessExpansionsAsync(
                expansionRequests!,
                topLevelToolGroupsByName,
                expandedGroupTools,
                expandedGroupToolGroups,
                expandedGroup,
                cancellationToken);

            augmentedHistory!.AddRange(addedMessages);
            responseMessages.AddRange(addedMessages);

            // Surface the expansion results to the caller as additional streaming updates.
            foreach (var message in addedMessages)
            {
                yield return FunctionInvokingChatClient.ConvertToolResultMessageToUpdate(message, response.ConversationId, toolMessageId);
            }

            // If a valid group was requested for expansion, and it does not match the currently-expanded group,
            // update the tools list to contain the newly-expanded tool group.
            (modifiedTools ??= []).Clear();
            modifiedTools.AddRange(baseTools);
            modifiedTools.AddRange(expandedGroupTools);
            modifiedOptions.Tools = modifiedTools;
            modifiedOptions.ConversationId = response.ConversationId;
        }
    }

    /// <summary>Extracts <see cref="AIToolGroup"/> instances from the provided options.</summary>
    private static List<AIToolGroup>? ExtractToolGroups(ChatOptions? options)
    {
        if (options?.Tools is not { Count: > 0 })
        {
            return null;
        }

        List<AIToolGroup>? groups = null;
        foreach (var tool in options.Tools)
        {
            if (tool is AIToolGroup group)
            {
                (groups ??= []).Add(group);
            }
        }

        return groups;
    }

    /// <summary>Creates a function that returns the list of available groups.</summary>
    private static string CreateListGroupsResult(
        AIToolGroup? expandedToolGroup,
        List<AIToolGroup> topLevelGroups,
        List<AIToolGroup>? nestedGroups)
    {
        var allToolGroups = nestedGroups is null
            ? topLevelGroups
            : topLevelGroups.Concat(nestedGroups);

        allToolGroups = allToolGroups.Where(g => g != expandedToolGroup);

        if (!allToolGroups.Any())
        {
            return "No tool groups are currently available.";
        }

        var sb = new StringBuilder();
        sb.Append("Available tool groups:");
        AppendAIToolList(sb, allToolGroups);
        return sb.ToString();
    }

    /// <summary>Processes expansion requests and returns messages to add, termination flag, and updated group state.</summary>
    private static async Task<(IList<ChatMessage> messagesToAdd, AIToolGroup? expandedGroup)> ProcessExpansionsAsync(
        List<FunctionCallContent> expansionRequests,
        Dictionary<string, AIToolGroup> topLevelGroupsByName,
        List<AITool> expandedGroupTools,
        List<AIToolGroup> expandedGroupToolGroups,
        AIToolGroup? expandedGroup,
        CancellationToken cancellationToken)
    {
        Debug.Assert(expansionRequests.Count != 0, "Expected at least one expansion request.");

        var contents = new List<AIContent>(expansionRequests.Count);

        foreach (var expansionRequest in expansionRequests)
        {
            if (expansionRequest.Arguments is not { Count: > 0 } arguments ||
                !arguments.TryGetValue(ExpansionFunctionGroupNameParameter, out var groupNameArg) ||
                groupNameArg is null)
            {
                contents.Add(new FunctionResultContent(
                    callId: expansionRequest.CallId,
                    result: "No group name was specified; ignoring expansion request."));
                continue;
            }

            bool TryGetValidToolGroup(string groupName, [NotNullWhen(true)] out AIToolGroup? group)
            {
                if (topLevelGroupsByName.TryGetValue(groupName, out group))
                {
                    return true;
                }

                group = expandedGroupToolGroups.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.Ordinal));
                return group is not null;
            }

            var groupName = groupNameArg.ToString();
            if (groupName is null || !TryGetValidToolGroup(groupName, out var group))
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

            // Expand the group
            expandedGroup = group;
            var groupTools = await group.GetToolsAsync(cancellationToken).ConfigureAwait(false);

            expandedGroupTools.Clear();
            expandedGroupToolGroups.Clear();

            foreach (var tool in groupTools)
            {
                if (tool is AIToolGroup toolGroup)
                {
                    expandedGroupToolGroups.Add(toolGroup);
                }
                else
                {
                    expandedGroupTools.Add(tool);
                }
            }

            // Build success message
            var sb = new StringBuilder();
            sb.Append("Successfully expanded group '");
            sb.Append(groupName);
            sb.Append("'.");

            if (expandedGroupTools.Count > 0)
            {
                sb.Append(" Only this group's tools are now available:");
                AppendAIToolList(sb, expandedGroupTools);
            }

            if (expandedGroupToolGroups.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Additional groups available for expansion:");
                AppendAIToolList(sb, expandedGroupToolGroups);
            }

            contents.Add(new FunctionResultContent(
                callId: expansionRequest.CallId,
                result: sb.ToString()));
        }

        return (messagesToAdd: [new ChatMessage(ChatRole.Tool, contents)], expandedGroup);
    }

    /// <summary>Appends a formatted list of AI tools to the specified <see cref="StringBuilder"/>.</summary>
    private static void AppendAIToolList(StringBuilder sb, IEnumerable<AITool> tools)
    {
        foreach (var tool in tools)
        {
            sb.AppendLine();
            sb.Append("- ");
            sb.Append(tool.Name);
            sb.Append(": ");
            sb.Append(tool.Description);
        }
    }

    /// <summary>Copies expansion requests from messages.</summary>
    private bool CopyExpansionRequests(IList<ChatMessage> messages, [NotNullWhen(true)] ref List<FunctionCallContent>? expansionRequests)
    {
        var any = false;
        foreach (var message in messages)
        {
            any |= CopyExpansionRequests(message.Contents, ref expansionRequests);
        }

        return any;
    }

    /// <summary>Copies expansion requests from contents.</summary>
    private bool CopyExpansionRequests(
        IList<AIContent> contents,
        [NotNullWhen(true)] ref List<FunctionCallContent>? expansionRequests)
    {
        var any = false;
        foreach (var content in contents)
        {
            if (content is FunctionCallContent functionCall &&
                string.Equals(functionCall.Name, _expansionFunction.Name, StringComparison.Ordinal))
            {
                (expansionRequests ??= []).Add(functionCall);
                any = true;
            }
        }

        return any;
    }

    /// <summary>
    /// Generates a list of base AI tools by combining the default expansion function with additional tools specified in
    /// the provided chat options, excluding any tools that are grouped.
    /// </summary>
    private List<AITool> ComputeBaseTools(ChatOptions? options, AIFunction listGroupsFunction)
    {
        List<AITool> baseTools = [listGroupsFunction, _expansionFunction];

        foreach (var tool in options?.Tools ?? [])
        {
            if (tool is not AIToolGroup)
            {
                if (string.Equals(tool.Name, _expansionFunction.Name, StringComparison.Ordinal) ||
                    string.Equals(tool.Name, listGroupsFunction.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The group expansion tool with name '{tool.Name}' collides with a registered tool of the same name.");
                }

                baseTools.Add(tool);
            }
        }

        return baseTools;
    }
}
