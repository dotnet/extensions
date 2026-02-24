// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using OpenAI.Responses;
using Xunit;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Microsoft.Extensions.AI;

public class OpenAIResponseClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.GetResponsesClient(TestRunnerConfiguration.Instance["OpenAI:ChatModel"] ?? "gpt-4o-mini")
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

        // Validate CodeInterpreterToolCallContent
        var toolCallContent = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolCallContent>().SingleOrDefault();
        Assert.NotNull(toolCallContent);
        Assert.NotNull(toolCallContent.CallId);
        Assert.NotEmpty(toolCallContent.CallId);
        Assert.NotNull(toolCallContent.Inputs);
        Assert.NotEmpty(toolCallContent.Inputs);

        var codeInput = toolCallContent.Inputs.OfType<DataContent>().FirstOrDefault();
        Assert.NotNull(codeInput);
        Assert.Equal("text/x-python", codeInput.MediaType);
        Assert.NotEmpty(codeInput.Data.ToArray());

        // Validate CodeInterpreterToolResultContent
        var toolResultContent = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolResultContent>().FirstOrDefault();
        Assert.NotNull(toolResultContent);
        Assert.NotNull(toolResultContent.CallId);
        Assert.NotEmpty(toolResultContent.CallId);

        if (toolResultContent.Outputs is not null)
        {
            Assert.NotEmpty(toolResultContent.Outputs);
            if (toolResultContent.Outputs.OfType<TextContent>().FirstOrDefault() is { } resultOutput)
            {
                Assert.NotEmpty(resultOutput.Text);
            }
        }
    }

    [ConditionalFact]
    public async Task UseWebSearch_AnnotationsReflectResults()
    {
        SkipIfNotEnabled();

        var response = await ChatClient.GetResponseAsync(
            "Write a paragraph about .NET based on at least three recent news articles. Cite your sources.",
            new() { Tools = [new HostedWebSearchTool()] });

        ChatMessage m = Assert.Single(response.Messages);

        // Verify that the web search tool call and result content are present.
        var wsCall = m.Contents.OfType<WebSearchToolCallContent>().FirstOrDefault();
        Assert.NotNull(wsCall);
        Assert.NotNull(wsCall.CallId);
        Assert.NotNull(wsCall.Queries);
        Assert.NotEmpty(wsCall.Queries);

        var wsResult = m.Contents.OfType<WebSearchToolResultContent>().FirstOrDefault();
        Assert.NotNull(wsResult);
        Assert.Equal(wsCall.CallId, wsResult.CallId);

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
            Tools = [new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp")) { ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire }],
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
                Tools = [new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp"))
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
                Tools = [new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp"))
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
            AllowBackgroundResponses = true,
        };

        // Get initial response with continuation token
        var response = await ChatClient.GetResponseAsync("What's the biggest animal?", chatOptions);
        Assert.NotNull(response.ContinuationToken);
        Assert.Empty(response.Messages);

        int attempts = 0;

        // Continue to poll until we get the final response
        while (response.ContinuationToken is not null && ++attempts < 10)
        {
            chatOptions.ContinuationToken = response.ContinuationToken;
            response = await ChatClient.GetResponseAsync([], chatOptions);
            await Task.Delay(1000);
        }

        Assert.Contains("whale", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetResponseAsync_BackgroundResponses_WithFunction()
    {
        SkipIfNotEnabled();

        int callCount = 0;

        using var chatClient = new FunctionInvokingChatClient(ChatClient);

        var chatOptions = new ChatOptions
        {
            AllowBackgroundResponses = true,
            Tools = [AIFunctionFactory.Create(() => { callCount++; return "5:43"; }, new AIFunctionFactoryOptions { Name = "GetCurrentTime" })]
        };

        // Get initial response with continuation token
        var response = await chatClient.GetResponseAsync("What time is it?", chatOptions);
        Assert.NotNull(response.ContinuationToken);
        Assert.Empty(response.Messages);

        int attempts = 0;

        // Poll until the result is received
        while (response.ContinuationToken is not null && ++attempts < 10)
        {
            chatOptions.ContinuationToken = response.ContinuationToken;

            response = await chatClient.GetResponseAsync([], chatOptions);
            await Task.Delay(1000);
        }

        Assert.Contains("5:43", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, callCount);
    }

    [ConditionalFact]
    public async Task GetStreamingResponseAsync_BackgroundResponses()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            AllowBackgroundResponses = true,
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
            AllowBackgroundResponses = true,
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
        chatOptions.ContinuationToken = continuationToken;
        await foreach (var update in ChatClient.GetStreamingResponseAsync([], chatOptions))
        {
            responseText += update;
        }

        Assert.Contains("Paris", responseText, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task GetStreamingResponseAsync_BackgroundResponses_WithFunction()
    {
        SkipIfNotEnabled();

        int callCount = 0;

        using var chatClient = new FunctionInvokingChatClient(ChatClient);

        var chatOptions = new ChatOptions
        {
            AllowBackgroundResponses = true,
            Tools = [AIFunctionFactory.Create(() => { callCount++; return "5:43"; }, new AIFunctionFactoryOptions { Name = "GetCurrentTime" })]
        };

        string responseText = "";

        await foreach (var update in chatClient.GetStreamingResponseAsync("What time is it?", chatOptions))
        {
            responseText += update;
        }

        Assert.Contains("5:43", responseText);
        Assert.Equal(1, callCount);
    }

    [ConditionalFact]
    public async Task RemoteMCP_Connector()
    {
        SkipIfNotEnabled();

        if (TestRunnerConfiguration.Instance["RemoteMCP:ConnectorAccessToken"] is not string accessToken)
        {
            throw new SkipTestException(
                "To run this test, set a value for RemoteMCP:ConnectorAccessToken. " +
                "You can obtain one by following https://platform.openai.com/docs/guides/tools-connectors-mcp?quickstart-panels=connector#authorizing-a-connector.");
        }

        await RunAsync(false, false);
        await RunAsync(true, true);

        async Task RunAsync(bool streaming, bool approval)
        {
            ChatOptions chatOptions = new()
            {
                Tools = [new HostedMcpServerTool("calendar", "connector_googlecalendar")
                    {
                        ApprovalMode = approval ?
                                HostedMcpServerToolApprovalMode.AlwaysRequire :
                                HostedMcpServerToolApprovalMode.NeverRequire,
                        AuthorizationToken = accessToken
                    }
                ],
            };

            using var client = CreateChatClient()!;

            List<ChatMessage> input = [new ChatMessage(ChatRole.User, "What is on my calendar for today?")];

            ChatResponse response = streaming ?
                await client.GetStreamingResponseAsync(input, chatOptions).ToChatResponseAsync() :
                await client.GetResponseAsync(input, chatOptions);

            if (approval)
            {
                input.AddRange(response.Messages);
                var approvalRequest = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolApprovalRequestContent>());
                Assert.Equal("search_events", approvalRequest.ToolCall.ToolName);
                input.Add(new ChatMessage(ChatRole.Tool, [approvalRequest.CreateResponse(true)]));

                response = streaming ?
                    await client.GetStreamingResponseAsync(input, chatOptions).ToChatResponseAsync() :
                    await client.GetResponseAsync(input, chatOptions);
            }

            Assert.NotNull(response);
            var toolCall = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolCallContent>());
            Assert.Equal("search_events", toolCall.ToolName);

            var toolResult = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolResultContent>());
            var content = Assert.IsType<TextContent>(Assert.Single(toolResult.Output!));
            Assert.Equal(@"{""events"": [], ""next_page_token"": null}", content.Text);
        }
    }

    [ConditionalFact]
    public async Task ToolCallResult_TextContent()
    {
        SkipIfNotEnabled();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((int a, int b) => new TextContent($"The sum is {a + b}"), "AddNumbers", "Adds two numbers together")]
        };

        using var client = new FunctionInvokingChatClient(ChatClient);

        var response = await client.GetResponseAsync("What is 25 plus 17? Use the AddNumbers function.", chatOptions);

        Assert.NotNull(response);

        // The model should have called the function and received "The sum is 42"
        Assert.Contains("42", response.Text);
    }

    [ConditionalFact]
    public async Task ToolCallResult_MultipleAIContents()
    {
        SkipIfNotEnabled();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((string city) => new List<AIContent>
            {
                new TextContent($"Weather in {city}: "),
                new TextContent("Sunny, 72°F")
            }, "GetWeather", "Gets the weather for a city")]
        };

        using var client = new FunctionInvokingChatClient(ChatClient);

        var response = await client.GetResponseAsync("What's the weather in Seattle? Use GetWeather.", chatOptions);

        Assert.NotNull(response);

        // Verify the function was called and both parts were included
        var messages = response.Messages.ToList();
        Assert.NotEmpty(messages);

        // Check that we got a response mentioning the weather data
        Assert.Contains("72", response.Text);
    }

    [ConditionalFact]
    public async Task ToolCallResult_ImageDataContent()
    {
        SkipIfNotEnabled();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => new DataContent(ImageDataUri.GetImageDataUri(), "image/png"), "GetDotnetLogo", "Returns the .NET logo image")]
        };

        using var client = new FunctionInvokingChatClient(ChatClient);

        var response = await client.GetResponseAsync("Call GetDotnetLogo and tell me what you see in the image.", chatOptions);

        Assert.NotNull(response);

        // The model should describe seeing the .NET logo or purple/related colors
        Assert.True(
            response.Text.Contains("logo", StringComparison.OrdinalIgnoreCase) ||
            response.Text.Contains("purple", StringComparison.OrdinalIgnoreCase) ||
            response.Text.Contains("dot", StringComparison.OrdinalIgnoreCase) ||
            response.Text.Contains("net", StringComparison.OrdinalIgnoreCase),
            $"Expected response to mention logo or colors, but got: {response.Text}");
    }

    [ConditionalFact]
    public async Task ToolCallResult_PdfDataContent()
    {
        SkipIfNotEnabled();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => new DataContent(ImageDataUri.GetPdfDataUri(), "application/pdf") { Name = "document.pdf" }, "GetDocument", "Returns a PDF document")]
        };

        using var client = new FunctionInvokingChatClient(ChatClient);

        var response = await client.GetResponseAsync("Call GetDocument and tell me what text is in the PDF.", chatOptions);

        Assert.NotNull(response);

        // The PDF contains "Hello World!" text
        Assert.Contains("Hello World", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task ToolCallResult_MixedContentWithImage()
    {
        SkipIfNotEnabled();

        var imageUri = ImageDataUri.GetImageDataUri();
        var imageBytes = Convert.FromBase64String(imageUri.ToString().Split(',')[1]);

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => new List<AIContent>
            {
                new TextContent("Analysis result: "),
                new DataContent(imageBytes, "image/png"),
                new TextContent(" - Image provided above")
            }, "AnalyzeImage", "Analyzes an image and returns results")]
        };

        using var client = new FunctionInvokingChatClient(ChatClient);

        var response = await client.GetResponseAsync("Call AnalyzeImage and describe what you see.", chatOptions);

        Assert.NotNull(response);

        // Should mention the analysis and describe the image
        Assert.True(
            response.Text.Contains("analysis", StringComparison.OrdinalIgnoreCase) ||
            response.Text.Contains("image", StringComparison.OrdinalIgnoreCase) ||
            response.Text.Contains("logo", StringComparison.OrdinalIgnoreCase),
            $"Expected response to mention analysis or image content, but got: {response.Text}");
    }

    [ConditionalFact]
    public async Task ReasoningContent_NonStreaming_RoundtripsEncryptedContent()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            RawRepresentationFactory = _ => new CreateResponseOptions
            {
                ReasoningOptions = new ResponseReasoningOptions
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Low,
                    ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                },
                StoredOutputEnabled = false,
                IncludedProperties = { IncludedResponseProperty.ReasoningEncryptedContent },
            },
        };

        // 1. First request: Get initial response with encrypted content
        List<ChatMessage> chatHistory = [new ChatMessage(ChatRole.User, "What is 2+2? Think step by step but be very brief.")];

        var response1 = await ChatClient.GetResponseAsync(chatHistory, chatOptions);
        Assert.NotNull(response1);

        // Verify we got reasoning content with encrypted data and RawRepresentation
        var reasoningContent = response1.Messages
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .FirstOrDefault();

        Assert.NotNull(reasoningContent);
        Assert.NotNull(reasoningContent.ProtectedData);
        Assert.NotEmpty(reasoningContent.ProtectedData);
        Assert.NotNull(reasoningContent.RawRepresentation);

        // 2. Second request: Uses raw representation
        chatHistory.AddMessages(response1);
        chatHistory.Add(new ChatMessage(ChatRole.User, "What is 3+3?"));

        var response2 = await ChatClient.GetResponseAsync(chatHistory, chatOptions);
        Assert.NotNull(response2);
        Assert.Contains("6", response2.Text);

        // 3. Serialize/deserialize to drop RawRepresentations, then make third request
        string json = JsonSerializer.Serialize(chatHistory, AIJsonUtilities.DefaultOptions);
        var deserializedHistory = JsonSerializer.Deserialize<List<ChatMessage>>(json, AIJsonUtilities.DefaultOptions)!;

        // Verify RawRepresentation was dropped but ProtectedData preserved
        var deserializedReasoning = deserializedHistory
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .First(r => !string.IsNullOrEmpty(r.ProtectedData));

        Assert.Null(deserializedReasoning.RawRepresentation);
        Assert.Equal(reasoningContent.ProtectedData, deserializedReasoning.ProtectedData);

        deserializedHistory.Add(new ChatMessage(ChatRole.User, "What is 4+4?"));

        var response3 = await ChatClient.GetResponseAsync(deserializedHistory, chatOptions);
        Assert.NotNull(response3);
        Assert.Contains("8", response3.Text);

        // 4. Corrupt the encrypted content and verify fourth request fails
        foreach (var reasoning in deserializedHistory
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .Where(r => !string.IsNullOrEmpty(r.ProtectedData)))
        {
            reasoning.ProtectedData = "completely_invalid_encrypted_content_that_should_fail";
        }

        deserializedHistory.Add(new ChatMessage(ChatRole.User, "What is 5+5?"));

        var ex = await Assert.ThrowsAsync<ClientResultException>(
            () => ChatClient.GetResponseAsync(deserializedHistory, chatOptions));
        Assert.Contains("encrypted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task ReasoningContent_Streaming_RoundtripsEncryptedContent()
    {
        // This test requires a reasoning model with encrypted content support.
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            RawRepresentationFactory = _ => new CreateResponseOptions
            {
                ReasoningOptions = new ResponseReasoningOptions
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Low,
                    ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                },
                StoredOutputEnabled = false,
                IncludedProperties = { IncludedResponseProperty.ReasoningEncryptedContent },
            },
        };

        // 1. First request: Get initial response with encrypted content via streaming
        List<ChatMessage> chatHistory = [new ChatMessage(ChatRole.User, "What is 2+2? Think step by step but be very brief.")];

        var response1 = await ChatClient.GetStreamingResponseAsync(chatHistory, chatOptions).ToChatResponseAsync();
        Assert.NotNull(response1);

        // Verify we got reasoning content with encrypted data
        // Note: After coalescing streaming updates, RawRepresentation is not preserved
        var reasoningContent = response1.Messages
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .FirstOrDefault();

        Assert.NotNull(reasoningContent);
        Assert.NotNull(reasoningContent.ProtectedData);
        Assert.NotEmpty(reasoningContent.ProtectedData);

        // 2. Second request: Uses the coalesced content (no RawRepresentation after coalescing)
        chatHistory.AddMessages(response1);
        chatHistory.Add(new ChatMessage(ChatRole.User, "What is 3+3?"));

        var response2 = await ChatClient.GetStreamingResponseAsync(chatHistory, chatOptions).ToChatResponseAsync();
        Assert.NotNull(response2);
        Assert.Contains("6", response2.Text);

        // 3. Serialize/deserialize to ensure ProtectedData survives, then make third request
        string json = JsonSerializer.Serialize(chatHistory, AIJsonUtilities.DefaultOptions);
        var deserializedHistory = JsonSerializer.Deserialize<List<ChatMessage>>(json, AIJsonUtilities.DefaultOptions)!;

        // Verify ProtectedData preserved after serialization
        var deserializedReasoning = deserializedHistory
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .First(r => !string.IsNullOrEmpty(r.ProtectedData));

        Assert.Null(deserializedReasoning.RawRepresentation);
        Assert.Equal(reasoningContent.ProtectedData, deserializedReasoning.ProtectedData);

        deserializedHistory.Add(new ChatMessage(ChatRole.User, "What is 4+4?"));

        var response3 = await ChatClient.GetStreamingResponseAsync(deserializedHistory, chatOptions).ToChatResponseAsync();
        Assert.NotNull(response3);
        Assert.Contains("8", response3.Text);

        // 4. Corrupt the encrypted content and verify fourth request fails
        foreach (var reasoning in deserializedHistory
            .SelectMany(m => m.Contents)
            .OfType<TextReasoningContent>()
            .Where(r => !string.IsNullOrEmpty(r.ProtectedData)))
        {
            reasoning.ProtectedData = "completely_invalid_encrypted_content_that_should_fail";
        }

        deserializedHistory.Add(new ChatMessage(ChatRole.User, "What is 5+5?"));

        var ex = await Assert.ThrowsAsync<ClientResultException>(async () =>
        {
            await foreach (var update in ChatClient.GetStreamingResponseAsync(deserializedHistory, chatOptions))
            {
                _ = update;
            }
        });
        Assert.Contains("encrypted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
