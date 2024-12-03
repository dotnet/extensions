// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.TestUtilities;
using OpenAI.RealtimeConversation;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeIntegrationTests
{
    private RealtimeConversationClient? _conversationClient;

    public OpenAIRealtimeIntegrationTests()
    {
        _conversationClient = CreateConversationClient();
    }

    [ConditionalFact]
    public async Task CanPerformFunctionCall()
    {
        SkipIfNotEnabled();

        var roomCapacityTool = AIFunctionFactory.Create(GetRoomCapacity);
        var sessionOptions = new ConversationSessionOptions
        {
            Instructions = "You help with booking appointments",
            Tools = { roomCapacityTool.ToConversationFunctionTool() },
            ContentModalities = ConversationContentModalities.Text,
        };

        using var session = await _conversationClient.StartConversationSessionAsync();
        await session.ConfigureSessionAsync(sessionOptions);

        await foreach (var update in session.ReceiveUpdatesAsync())
        {
            switch (update)
            {
                case ConversationSessionStartedUpdate:
                    await session.AddItemAsync(
                        ConversationItem.CreateUserMessage(["""
                            What type of room can hold the most people?
                            Reply with the full name of the biggest venue and its capacity only.
                            Do not mention the other venues.
                        """]));
                    await session.StartResponseAsync();
                    break;

                case ConversationResponseFinishedUpdate responseFinished:
                    var content = responseFinished.CreatedItems
                        .SelectMany(i => i.MessageContentParts ?? [])
                        .OfType<ConversationContentPart>()
                        .FirstOrDefault();
                    if (content is not null)
                    {
                        Assert.Contains("VehicleAssemblyBuilding", content.Text.Replace(" ", string.Empty));
                        Assert.Contains("12000", content.Text.Replace(",", string.Empty));
                        return;
                    }

                    break;
            }

            await session.HandleToolCallsAsync(update, [roomCapacityTool]);
        }
    }

    [Description("Returns the number of people that can fit in a room.")]
    private static int GetRoomCapacity(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.ShuttleSimulator => throw new InvalidOperationException("No longer available"),
            RoomType.NorthAtlantisLawn => 450,
            RoomType.VehicleAssemblyBuilding => 12000,
            _ => throw new NotSupportedException($"Unknown room type: {roomType}"),
        };
    }

    private enum RoomType
    {
        ShuttleSimulator,
        NorthAtlantisLawn,
        VehicleAssemblyBuilding,
    }

    [MemberNotNull(nameof(_conversationClient))]
    protected void SkipIfNotEnabled()
    {
        if (_conversationClient is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }

    private static RealtimeConversationClient? CreateConversationClient()
    {
        var realtimeModel = TestRunnerConfiguration.Instance["OpenAI:RealtimeModel"];
        if (string.IsNullOrEmpty(realtimeModel))
        {
            return null;
        }

        var openAiClient = (AzureOpenAIClient?)IntegrationTestHelpers.GetOpenAIClient();
        return openAiClient?.GetRealtimeConversationClient(realtimeModel);
    }
}
