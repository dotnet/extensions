// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using OpenAI.Responses;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAIResponseClientTests
{
    [Fact]
    public void AsIChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("responseClient", () => ((OpenAIResponseClient)null!).AsIChatClient());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsIChatClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var client = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IChatClient chatClient = client.GetOpenAIResponseClient(model).AsIChatClient();
        var metadata = chatClient.GetService<ChatClientMetadata>();
        Assert.Equal("openai", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_SuccessfullyReturnsUnderlyingClient()
    {
        OpenAIResponseClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetOpenAIResponseClient("model");
        IChatClient chatClient = openAIClient.AsIChatClient();

        Assert.Same(chatClient, chatClient.GetService<IChatClient>());
        Assert.Same(openAIClient, chatClient.GetService<OpenAIResponseClient>());

        using IChatClient pipeline = chatClient
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .Build();

        Assert.NotNull(pipeline.GetService<FunctionInvokingChatClient>());
        Assert.NotNull(pipeline.GetService<DistributedCachingChatClient>());
        Assert.NotNull(pipeline.GetService<CachingChatClient>());
        Assert.NotNull(pipeline.GetService<OpenTelemetryChatClient>());

        Assert.Same(openAIClient, pipeline.GetService<OpenAIResponseClient>());
        Assert.IsType<FunctionInvokingChatClient>(pipeline.GetService<IChatClient>());
    }

    [Fact]
    public async Task BasicRequestResponse_NonStreaming()
    {
        const string Input = """
            {
                "temperature":0.5,
                "model":"gpt-4o-mini",
                "input": [{
                    "type":"message",
                    "role":"user",
                    "content":[{"type":"input_text","text":"hello"}]
                }],
                "max_output_tokens":20
            }
            """;

        const string Output = """
            {
              "id": "resp_67d327649b288191aeb46a824e49dc40058a5e08c46a181d",
              "object": "response",
              "created_at": 1741891428,
              "status": "completed",
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": 20,
              "model": "gpt-4o-mini-2024-07-18",
              "output": [
                {
                  "type": "message",
                  "id": "msg_67d32764fcdc8191bcf2e444d4088804058a5e08c46a181d",
                  "status": "completed",
                  "role": "assistant",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "Hello! How can I assist you today?",
                      "annotations": []
                    }
                  ]
                }
              ],
              "parallel_tool_calls": true,
              "previous_response_id": null,
              "reasoning": {
                "effort": null,
                "generate_summary": null
              },
              "store": true,
              "temperature": 0.5,
              "text": {
                "format": {
                  "type": "text"
                }
              },
              "tool_choice": "auto",
              "tools": [],
              "top_p": 1.0,
              "usage": {
                "input_tokens": 26,
                "input_tokens_details": {
                  "cached_tokens": 0
                },
                "output_tokens": 10,
                "output_tokens_details": {
                  "reasoning_tokens": 0
                },
                "total_tokens": 36
              },
              "user": null,
              "metadata": {}
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("hello", new()
        {
            MaxOutputTokens = 20,
            Temperature = 0.5f,
        });
        Assert.NotNull(response);

        Assert.Equal("resp_67d327649b288191aeb46a824e49dc40058a5e08c46a181d", response.ResponseId);
        Assert.Equal("resp_67d327649b288191aeb46a824e49dc40058a5e08c46a181d", response.ConversationId);
        Assert.Equal("Hello! How can I assist you today?", response.Text);
        Assert.Single(response.Messages.Single().Contents);
        Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_741_891_428), response.CreatedAt);
        Assert.Null(response.FinishReason);

        Assert.NotNull(response.Usage);
        Assert.Equal(26, response.Usage.InputTokenCount);
        Assert.Equal(10, response.Usage.OutputTokenCount);
        Assert.Equal(36, response.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task BasicRequestResponse_Streaming()
    {
        const string Input = """
            {
                "temperature":0.5,
                "model":"gpt-4o-mini",
                "input":[
                    {
                        "type":"message",
                        "role":"user",
                        "content":[{"type":"input_text","text":"hello"}]
                    }
                ],
                "stream":true,
                "max_output_tokens":20
            }
            """;

        const string Output = """
            event: response.created
            data: {"type":"response.created","response":{"id":"resp_67d329fbc87c81919f8952fe71dafc96029dabe3ee19bb77","object":"response","created_at":1741892091,"status":"in_progress","error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":20,"model":"gpt-4o-mini-2024-07-18","output":[],"parallel_tool_calls":true,"previous_response_id":null,"reasoning":{"effort":null,"generate_summary":null},"store":true,"temperature":0.5,"text":{"format":{"type":"text"}},"tool_choice":"auto","tools":[],"top_p":1.0,"usage":null,"user":null,"metadata":{}}}

            event: response.in_progress
            data: {"type":"response.in_progress","response":{"id":"resp_67d329fbc87c81919f8952fe71dafc96029dabe3ee19bb77","object":"response","created_at":1741892091,"status":"in_progress","error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":20,"model":"gpt-4o-mini-2024-07-18","output":[],"parallel_tool_calls":true,"previous_response_id":null,"reasoning":{"effort":null,"generate_summary":null},"store":true,"temperature":0.5,"text":{"format":{"type":"text"}},"tool_choice":"auto","tools":[],"top_p":1.0,"usage":null,"user":null,"metadata":{}}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","output_index":0,"item":{"type":"message","id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","status":"in_progress","role":"assistant","content":[]}}

            event: response.content_part.added
            data: {"type":"response.content_part.added","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"part":{"type":"output_text","text":"","annotations":[]}}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":"Hello"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":"!"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" How"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" can"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" I"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" assist"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" you"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":" today"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"delta":"?"}

            event: response.output_text.done
            data: {"type":"response.output_text.done","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"text":"Hello! How can I assist you today?"}

            event: response.content_part.done
            data: {"type":"response.content_part.done","item_id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","output_index":0,"content_index":0,"part":{"type":"output_text","text":"Hello! How can I assist you today?","annotations":[]}}

            event: response.output_item.done
            data: {"type":"response.output_item.done","output_index":0,"item":{"type":"message","id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","status":"completed","role":"assistant","content":[{"type":"output_text","text":"Hello! How can I assist you today?","annotations":[]}]}}

            event: response.completed
            data: {"type":"response.completed","response":{"id":"resp_67d329fbc87c81919f8952fe71dafc96029dabe3ee19bb77","object":"response","created_at":1741892091,"status":"completed","error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":20,"model":"gpt-4o-mini-2024-07-18","output":[{"type":"message","id":"msg_67d329fc0c0081919696b8ab36713a41029dabe3ee19bb77","status":"completed","role":"assistant","content":[{"type":"output_text","text":"Hello! How can I assist you today?","annotations":[]}]}],"parallel_tool_calls":true,"previous_response_id":null,"reasoning":{"effort":null,"generate_summary":null},"store":true,"temperature":0.5,"text":{"format":{"type":"text"}},"tool_choice":"auto","tools":[],"top_p":1.0,"usage":{"input_tokens":26,"input_tokens_details":{"cached_tokens":0},"output_tokens":10,"output_tokens_details":{"reasoning_tokens":0},"total_tokens":36},"user":null,"metadata":{}}}


            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("hello", new()
        {
            MaxOutputTokens = 20,
            Temperature = 0.5f,
        }))
        {
            updates.Add(update);
        }

        Assert.Equal("Hello! How can I assist you today?", string.Concat(updates.Select(u => u.Text)));

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_741_892_091);
        Assert.Equal(17, updates.Count);

        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("resp_67d329fbc87c81919f8952fe71dafc96029dabe3ee19bb77", updates[i].ResponseId);
            Assert.Equal("resp_67d329fbc87c81919f8952fe71dafc96029dabe3ee19bb77", updates[i].ConversationId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-mini-2024-07-18", updates[i].ModelId);
            Assert.Null(updates[i].AdditionalProperties);
            Assert.Equal((i >= 4 && i <= 12) || i == 16 ? 1 : 0, updates[i].Contents.Count);
            Assert.Equal(i < updates.Count - 1 ? null : ChatFinishReason.Stop, updates[i].FinishReason);
        }

        for (int i = 4; i < updates.Count; i++)
        {
            Assert.Equal(ChatRole.Assistant, updates[i].Role);
        }

        UsageContent usage = updates.SelectMany(u => u.Contents).OfType<UsageContent>().Single();
        Assert.Equal(26, usage.Details.InputTokenCount);
        Assert.Equal(10, usage.Details.OutputTokenCount);
        Assert.Equal(36, usage.Details.TotalTokenCount);
    }

    [Fact]
    public async Task ChatOptions_DoNotOverwrite_NotNullPropertiesInRawRepresentation_NonStreaming()
    {
        const string Input = """
            {
              "input":[{"type":"message","role":"user","content":[{"type":"input_text","text":"hello"}]}],
              "model":"gpt-4o-mini",
              "max_output_tokens":10,
              "previous_response_id":"resp_42",
              "top_p":0.5,
              "temperature":0.5,
              "parallel_tool_calls":true,
              "text": {"format": {"type": "text"}
            },
              "tool_choice":"auto",
              "tools":[
                {"description":"Gets the age of the specified person.","name":"GetPersonAge","parameters":{"additionalProperties":false,"properties":{"personName":{"description":"The person whose age is being requested","type":"string"}},"required":["personName"],"type":"object"},"strict":false,"type":"function"},
                {"description":"Gets the age of the specified person.","name":"GetPersonAge","parameters":{"additionalProperties":false,"properties":{"personName":{"description":"The person whose age is being requested","type":"string"}},"required":["personName"],"type":"object"},"strict":false,"type":"function"}
              ]
            }
            """;

        const string Output = """
            {
              "id": "resp_67d327649b288191aeb46a824e49dc40058a5e08c46a181d",
              "object": "response",
              "created_at": 1741891428,
              "status": "completed",
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": 20,
              "model": "gpt-4o-mini-2024-07-18",
              "output": [
                {
                  "type": "message",
                  "id": "msg_67d32764fcdc8191bcf2e444d4088804058a5e08c46a181d",
                  "status": "completed",
                  "role": "assistant",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "Hello! How can I assist you today?",
                      "annotations": []
                    }
                  ]
                }
              ],
              "parallel_tool_calls": true,
              "previous_response_id": null,
              "reasoning": {
                "effort": null,
                "generate_summary": null
              },
              "store": true,
              "temperature": 0.5,
              "text": {
                "format": {
                  "type": "text"
                }
              },
              "tool_choice": "auto",
              "tools": [],
              "top_p": 1.0,
              "usage": {
                "input_tokens": 26,
                "input_tokens_details": {
                  "cached_tokens": 0
                },
                "output_tokens": 10,
                "output_tokens_details": {
                  "reasoning_tokens": 0
                },
                "total_tokens": 36
              },
              "user": null,
              "metadata": {}
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, modelId: "gpt-4o-mini");
        AIFunction tool = AIFunctionFactory.Create(([Description("The person whose age is being requested")] string personName) => 42, "GetPersonAge", "Gets the age of the specified person.");

        ChatOptions chatOptions = new()
        {
            RawRepresentationFactory = (c) =>
            {
                ResponseCreationOptions openAIOptions = new()
                {
                    MaxOutputTokenCount = 10,
                    PreviousResponseId = "resp_42",
                    TopP = 0.5f,
                    Temperature = 0.5f,
                    ParallelToolCallsEnabled = true,
                    ToolChoice = ResponseToolChoice.CreateAutoChoice(),
                    TextOptions = new ResponseTextOptions
                    {
                        TextFormat = ResponseTextFormat.CreateTextFormat()
                    },
                };
                openAIOptions.Tools.Add(ToOpenAIResponseChatTool(tool));
                return openAIOptions;
            },
            ModelId = null,
            MaxOutputTokens = 1,
            ConversationId = "foo",
            TopP = 0.125f,
            Temperature = 0.125f,
            AllowMultipleToolCalls = false,
            Tools = [tool],
            ToolMode = ChatToolMode.None,
            ResponseFormat = ChatResponseFormat.Json
        };

        var response = await client.GetResponseAsync("hello", chatOptions);
        Assert.NotNull(response);
        Assert.Equal("Hello! How can I assist you today?", response.Text);
    }

    /// <summary>Converts an Extensions function to an OpenAI response chat tool.</summary>
    private static ResponseTool ToOpenAIResponseChatTool(AIFunction aiFunction)
    {
        var tool = JsonSerializer.Deserialize<OpenAIChatClientTests.ChatToolJson>(aiFunction.JsonSchema)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool));
        return ResponseTool.CreateFunctionTool(aiFunction.Name, aiFunction.Description, functionParameters);
    }

    private static IChatClient CreateResponseClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(
            new ApiKeyCredential("apikey"),
            new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetOpenAIResponseClient(modelId)
        .AsIChatClient();
}
