// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIResponseClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.GetOpenAIResponseClient(TestRunnerConfiguration.Instance["OpenAI:ChatModel"] ?? "gpt-4o-mini")
        .AsIChatClient();

    public override bool FunctionInvokingChatClientSetsConversationId => true;

    // Test structure doesn't make sense with Responses.
    public override Task Caching_AfterFunctionInvocation_FunctionOutputUnchangedAsync() => Task.CompletedTask;

    [ConditionalFact]
    public async Task UseCodeInterpreter_ProducesCodeExecutionResults()
    {
        SkipIfNotEnabled();

        var response = await ChatClient.GetResponseAsync("Use the code interpreter to calculate the square root of 42. Return only the nearest integer value and no other text.", new()
        {
            Tools = [new HostedCodeInterpreterTool()],
        });
        Assert.NotNull(response);

        ChatMessage message = Assert.Single(response.Messages);

        Assert.Equal("6", message.Text);
    }

    [ConditionalFact]
    public async Task UseWebSearch_AnnotationsReflectResults()
    {
        SkipIfNotEnabled();

        var response = await ChatClient.GetResponseAsync(
            "Write a paragraph about .NET based on at least three recent news articles. Cite your sources.",
            new() { Tools = [new HostedWebSearchTool()] });

        ChatMessage m = Assert.Single(response.Messages);
        TextContent tc = m.Contents.OfType<TextContent>().First();
        Assert.NotNull(tc.Annotations);
        Assert.NotEmpty(tc.Annotations);
        Assert.All(tc.Annotations, a =>
        {
            CitationAnnotation ca = Assert.IsType<CitationAnnotation>(a);
            var regions = Assert.IsType<List<AnnotatedRegion>>(ca.AnnotatedRegions);
            Assert.NotNull(regions);
            Assert.Single(regions);
            var region = Assert.IsType<TextSpanAnnotatedRegion>(regions[0]);
            Assert.NotNull(region);
            Assert.NotNull(region.StartIndex);
            Assert.NotNull(region.EndIndex);
            Assert.NotNull(ca.Url);
            Assert.NotNull(ca.Title);
            Assert.NotEmpty(ca.Title);
        });
    }

    [ConditionalFact]
    public async Task RemoteMCP_ListTools()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            Tools = [new HostedMcpServerTool("deepwiki", "https://mcp.deepwiki.com/mcp") { ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire }],
        };

        ChatResponse response = await CreateChatClient()!.GetResponseAsync("Which tools are available on the wiki_tools MCP server?", chatOptions);

        Assert.Contains("read_wiki_structure", response.Text);
        Assert.Contains("read_wiki_contents", response.Text);
        Assert.Contains("ask_question", response.Text);
    }

    [ConditionalFact]
    public async Task RemoteMCP_CallTool_ApprovalNeverRequired()
    {
        SkipIfNotEnabled();

        await RunAsync(false, false);
        await RunAsync(true, true);

        async Task RunAsync(bool streaming, bool requireSpecific)
        {
            ChatOptions chatOptions = new()
            {
                Tools = [new HostedMcpServerTool("deepwiki", "https://mcp.deepwiki.com/mcp")
                    {
                        ApprovalMode = requireSpecific ?
                            HostedMcpServerToolApprovalMode.RequireSpecific(null, ["read_wiki_structure", "ask_question"]) :
                            HostedMcpServerToolApprovalMode.NeverRequire,
                    }
                ],
            };

            using var client = CreateChatClient()!;

            const string Prompt = "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository";

            ChatResponse response = streaming ?
                await client.GetStreamingResponseAsync(Prompt, chatOptions).ToChatResponseAsync() :
                await client.GetResponseAsync(Prompt, chatOptions);

            Assert.NotNull(response);
            Assert.NotEmpty(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolCallContent>());
            Assert.NotEmpty(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolResultContent>());
            Assert.Empty(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolApprovalRequestContent>());

            Assert.Contains("src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md", response.Text);
        }
    }

    [ConditionalFact]
    public async Task RemoteMCP_CallTool_ApprovalRequired()
    {
        SkipIfNotEnabled();

        await RunAsync(false, false, false);
        await RunAsync(true, true, false);
        await RunAsync(false, false, true);
        await RunAsync(true, true, true);

        async Task RunAsync(bool streaming, bool requireSpecific, bool useConversationId)
        {
            ChatOptions chatOptions = new()
            {
                Tools = [new HostedMcpServerTool("deepwiki", "https://mcp.deepwiki.com/mcp")
                    {
                        ApprovalMode = requireSpecific ?
                            HostedMcpServerToolApprovalMode.RequireSpecific(["read_wiki_structure", "ask_question"], null) :
                            HostedMcpServerToolApprovalMode.AlwaysRequire,
                    }
                ],
            };

            using var client = CreateChatClient()!;

            // Initial request
            List<ChatMessage> input = [new ChatMessage(ChatRole.User, "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository")];
            ChatResponse response = streaming ?
                await client.GetStreamingResponseAsync(input, chatOptions).ToChatResponseAsync() :
                await client.GetResponseAsync(input, chatOptions);

            // Handle approvals of up to two rounds of tool calls
            int approvalsCount = 0;
            for (int i = 0; i < 2; i++)
            {
                if (useConversationId)
                {
                    chatOptions.ConversationId = response.ConversationId;
                    input.Clear();
                }
                else
                {
                    input.AddRange(response.Messages);
                }

                var approvalResponse = new ChatMessage(ChatRole.Tool,
                    response.Messages
                            .SelectMany(m => m.Contents)
                            .OfType<McpServerToolApprovalRequestContent>()
                            .Select(c => new McpServerToolApprovalResponseContent(c.ToolCall.CallId, true))
                            .ToArray());
                if (approvalResponse.Contents.Count == 0)
                {
                    break;
                }

                approvalsCount += approvalResponse.Contents.Count;
                input.Add(approvalResponse);
                response = streaming ?
                    await client.GetStreamingResponseAsync(input, chatOptions).ToChatResponseAsync() :
                    await client.GetResponseAsync(input, chatOptions);
            }

            // Validate final response
            Assert.Equal(2, approvalsCount);
            Assert.Contains("src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md", response.Text);
        }
    }

    [ConditionalFact]
    public async Task GetResponseAsync_BackgroundResponses()
    {
        SkipIfNotEnabled();

        var chatOptions = new ChatOptions
        {
            BackgroundResponsesOptions = new() { Allow = true }
        };

        // Get initial response with continuation token
        var response = await ChatClient.GetResponseAsync("What's the biggest animal?", chatOptions);
        Assert.NotNull(response.ContinuationToken);
        Assert.Empty(response.Messages);

        int attempts = 0;

        // Continue to poll until we get the final response
        while (response.ContinuationToken is not null && ++attempts < 10)
        {
            response = await ChatClient.GetResponseAsync(response.ContinuationToken, chatOptions);
            await Task.Delay(500);
        }

        Assert.Contains("whale", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetResponseAsync_BackgroundResponses_WithFunction()
    {
        SkipIfNotEnabled();

        using var chatClient = new FunctionInvokingChatClient(ChatClient);

        var chatOptions = new ChatOptions
        {
            BackgroundResponsesOptions = new() { Allow = true },
            Tools = [AIFunctionFactory.Create(() => "5:43", new AIFunctionFactoryOptions { Name = "GetCurrentTime" })]
        };

        // Get initial response with continuation token
        var response = await chatClient.GetResponseAsync("What time is it?", chatOptions);
        Assert.NotNull(response.ContinuationToken);
        Assert.Empty(response.Messages);

        int attempts = 0;

        // Poll until the result is received
        while (response.ContinuationToken is not null && ++attempts < 10)
        {
            response = await chatClient.GetResponseAsync(response.ContinuationToken, chatOptions);
            await Task.Delay(1000);
        }

        Assert.Contains("5:43", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetStreamingResponseAsync_BackgroundResponses()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            BackgroundResponsesOptions = new BackgroundResponsesOptions { Allow = true },
        };

        string responseText = "";

        await foreach (var update in ChatClient.GetStreamingResponseAsync("What is the capital of France?", chatOptions))
        {
            responseText += update;
        }

        // Assert
        Assert.Contains("Paris", responseText, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetStreamingResponseAsync_BackgroundResponses_StreamResumption()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            BackgroundResponsesOptions = new BackgroundResponsesOptions { Allow = true },
        };

        int updateNumber = 0;
        string responseText = "";
        ResponseContinuationToken? continuationToken = null;

        await foreach (var update in ChatClient.GetStreamingResponseAsync("What is the capital of France?", chatOptions))
        {
            responseText += update;

            // Simulate an interruption after receiving 8 updates.
            if (updateNumber++ == 8)
            {
                continuationToken = update.ContinuationToken;
                break;
            }
        }

        Assert.DoesNotContain("Paris", responseText);

        // Resume streaming from the point of interruption captured by the continuation token.
        await foreach (var update in ChatClient.GetStreamingResponseAsync(continuationToken!, chatOptions))
        {
            responseText += update;
        }

        Assert.Contains("Paris", responseText, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetStreamingResponseAsync_BackgroundResponses_WithFunction()
    {
        SkipIfNotEnabled();

        using var chatClient = new FunctionInvokingChatClient(ChatClient);

        var chatOptions = new ChatOptions
        {
            BackgroundResponsesOptions = new() { Allow = true },
            Tools = [AIFunctionFactory.Create(() => "5:43", new AIFunctionFactoryOptions { Name = "GetCurrentTime" })]
        };

        string responseText = "";

        await foreach (var update in chatClient.GetStreamingResponseAsync("What time is it?", chatOptions))
        {
            responseText += update;
        }

        Assert.Contains("5:43", responseText);
    }
}
