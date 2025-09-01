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

    [Fact]
    public void AsIChatClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

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
    public async Task BasicReasoningResponse_Streaming()
    {
        const string Input = """
            {
              "input":[{
                "type":"message",
                "role":"user",
                "content":[{
                  "type":"input_text",
                  "text":"Calculate the sum of the first 5 positive integers."
                }]
              }],
              "reasoning": {
                "summary": "detailed",
                "effort": "low"
              },
              "model": "o4-mini",
              "stream": true
            }
            """;

        // Compressed down for testing purposes; real-world output would be larger.
        const string Output = """
            event: response.created
            data: {"type":"response.created","sequence_number":0,"response":{"id":"resp_68b5ebab461881969ed94149372c2a530698ecbf1b9f2704","object":"response","created_at":1756752811,"status":"in_progress","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"o4-mini-2025-04-16","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":"low","summary":"detailed"},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.in_progress
            data: {"type":"response.in_progress","sequence_number":1,"response":{"id":"resp_68b5ebab461881969ed94149372c2a530698ecbf1b9f2704","object":"response","created_at":1756752811,"status":"in_progress","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"o4-mini-2025-04-16","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":"low","summary":"detailed"},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":2,"output_index":0,"item":{"id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","type":"reasoning","summary":[]}}

            event: response.reasoning_summary_part.added
            data: {"type":"response.reasoning_summary_part.added","sequence_number":3,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"part":{"type":"summary_text","text":""}}

            event: response.reasoning_summary_text.delta
            data: {"type":"response.reasoning_summary_text.delta","sequence_number":4,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"delta":"**Calcul","obfuscation":"sLkbFySM"}

            event: response.reasoning_summary_text.delta
            data: {"type":"response.reasoning_summary_text.delta","sequence_number":5,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"delta":"ating","obfuscation":"dkm1f6DKqUj"}

            event: response.reasoning_summary_text.delta
            data: {"type":"response.reasoning_summary_text.delta","sequence_number":6,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"delta":" a","obfuscation":"X8ahc2lfCf9eA1"}

            event: response.reasoning_summary_text.delta
            data: {"type":"response.reasoning_summary_text.delta","sequence_number":7,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"delta":" simple","obfuscation":"1rLVyIaNl"}

            event: response.reasoning_summary_text.delta
            data: {"type":"response.reasoning_summary_text.delta","sequence_number":8,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"delta":" sum","obfuscation":"jCK7mgNR80Re"}

            event: response.reasoning_summary_text.done
            data: {"type":"response.reasoning_summary_text.done","sequence_number":9,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"text":"**Calculating a simple sum**"}

            event: response.reasoning_summary_part.done
            data: {"type":"response.reasoning_summary_part.done","sequence_number":10,"item_id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","output_index":0,"summary_index":0,"part":{"type":"summary_text","text":"**Calculating a simple sum**"}}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":11,"output_index":0,"item":{"id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","type":"reasoning","summary":[{"type":"summary_text","text":"**Calculating a simple sum**"}]}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":12,"output_index":1,"item":{"id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","type":"message","status":"in_progress","content":[],"role":"assistant"}}

            event: response.content_part.added
            data: {"type":"response.content_part.added","sequence_number":13,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"part":{"type":"output_text","annotations":[],"logprobs":[],"text":""}}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":14,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":"The","logprobs":[],"obfuscation":"japg2KaCkjNsp"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":15,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" sum","logprobs":[],"obfuscation":"1BEqjKQ0KU41"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":16,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" of","logprobs":[],"obfuscation":"GUqom1rsdZsnT"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":17,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" the","logprobs":[],"obfuscation":"UmCms91yrTlg"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":18,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" first","logprobs":[],"obfuscation":"AyNbZpfTXo"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":19,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" ","logprobs":[],"obfuscation":"tuyz4HkKODFQRtk"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":20,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":"5","logprobs":[],"obfuscation":"QAwyISolmjXfTlc"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":21,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" positive","logprobs":[],"obfuscation":"2Euge1H"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":22,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" integers","logprobs":[],"obfuscation":"ih0Znt8"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":23,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" is","logprobs":[],"obfuscation":"oQihR5Pw8jRz5"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":24,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":" 15","logprobs":[],"obfuscation":"7TdJ1FWlZF8lTd"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":25,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"delta":".","logprobs":[],"obfuscation":"x2VAJKlWI8qjgYq"}

            event: response.output_text.done
            data: {"type":"response.output_text.done","sequence_number":26,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"text":"The sum of the first 5 positive integers is 15.","logprobs":[]}

            event: response.content_part.done
            data: {"type":"response.content_part.done","sequence_number":27,"item_id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","output_index":1,"content_index":0,"part":{"type":"output_text","annotations":[],"logprobs":[],"text":"The sum of the first 5 positive integers is 15."}}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":28,"output_index":1,"item":{"id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","type":"message","status":"completed","content":[{"type":"output_text","annotations":[],"logprobs":[],"text":"The sum of the first 5 positive integers is 15."}],"role":"assistant"}}

            event: response.completed
            data: {"type":"response.completed","sequence_number":29,"response":{"id":"resp_68b5ebab461881969ed94149372c2a530698ecbf1b9f2704","object":"response","created_at":1756752811,"status":"completed","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"o4-mini-2025-04-16","output":[{"id":"rs_68b5ebabc0088196afb9fa86b487732d0698ecbf1b9f2704","type":"reasoning","summary":[{"type":"summary_text","text":"**Calculating a simple sum**"}]},{"id":"msg_68b5ebae5a708196b74b94f22ca8995e0698ecbf1b9f2704","type":"message","status":"completed","content":[{"type":"output_text","annotations":[],"logprobs":[],"text":"The sum of the first 5 positive integers is 15."}],"role":"assistant"}],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":"low","summary":"detailed"},"safety_identifier":null,"service_tier":"default","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":{"input_tokens":17,"input_tokens_details":{"cached_tokens":0},"output_tokens":122,"output_tokens_details":{"reasoning_tokens":64},"total_tokens":139},"user":null,"metadata":{}}}


            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "o4-mini");

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("Calculate the sum of the first 5 positive integers.", new()
        {
            RawRepresentationFactory = options => new ResponseCreationOptions
            {
                ReasoningOptions = new()
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Low,
                    ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                }
            }
        }))
        {
            updates.Add(update);
        }

        Assert.Equal("The sum of the first 5 positive integers is 15.", string.Concat(updates.Select(u => u.Text)));

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_756_752_811);
        Assert.Equal(30, updates.Count);

        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("resp_68b5ebab461881969ed94149372c2a530698ecbf1b9f2704", updates[i].ResponseId);
            Assert.Equal("resp_68b5ebab461881969ed94149372c2a530698ecbf1b9f2704", updates[i].ConversationId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("o4-mini-2025-04-16", updates[i].ModelId);
            Assert.Null(updates[i].AdditionalProperties);

            if (i is (>= 4 and <= 8))
            {
                // Reasoning updates
                Assert.Single(updates[i].Contents);
            }
            else if (i is (>= 14 and <= 25) or 29)
            {
                // Response Complete and Assistant message updates
                Assert.Single(updates[i].Contents);
            }
            else
            {
                // Other updates
                Assert.Empty(updates[i].Contents);
            }

            Assert.Equal(i < updates.Count - 1 ? null : ChatFinishReason.Stop, updates[i].FinishReason);
        }

        // Reasoning Content Check
        for (int i = 4; i <= 8; i++)
        {
            Assert.Null(updates[i].Role);
            var reasoning = Assert.IsType<TextReasoningContent>(updates[i].Contents.Single());
            Assert.NotNull(reasoning);
            Assert.NotNull(reasoning.Text);
        }

        UsageContent usage = updates.SelectMany(u => u.Contents).OfType<UsageContent>().Single();
        Assert.Equal(17, usage.Details.InputTokenCount);
        Assert.Equal(122, usage.Details.OutputTokenCount);
        Assert.Equal(139, usage.Details.TotalTokenCount);
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
    public async Task ChatOptions_StrictRespected()
    {
        const string Input = """
            {
                "model": "gpt-4o-mini",
                "input": [
                    {
                        "type": "message",
                        "role": "user",
                        "content": [
                            {
                                "type": "input_text",
                                "text": "hello"
                            }
                        ]
                    }
                ],
                "tool_choice": "auto",
                "tools": [
                    {
                        "type": "function",
                        "name": "GetPersonAge",
                        "description": "Gets the age of the specified person.",
                        "parameters": {
                            "type": "object",
                            "required": [],
                            "properties": {},
                            "additionalProperties": false
                        },
                        "strict": true
                    }
                ]
            }
            """;

        const string Output = """
            {
              "id": "resp_67d327649b288191aeb46a824e49dc40058a5e08c46a181d",
              "object": "response",
              "status": "completed",
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
              ]
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        var response = await client.GetResponseAsync("hello", new()
        {
            Tools = [AIFunctionFactory.Create(() => 42, "GetPersonAge", "Gets the age of the specified person.")],
            AdditionalProperties = new()
            {
                ["strictJsonSchema"] = true,
            },
        });
        Assert.NotNull(response);
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

    [Fact]
    public async Task MultipleOutputItems_NonStreaming()
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
                      "text": "Hello!",
                      "annotations": []
                    }
                  ]
                },
                {
                  "type": "message",
                  "id": "msg_67d32764fcdc8191bcf2e444d4088804058a5e08c46a182e",
                  "status": "completed",
                  "role": "assistant",
                  "content": [
                    {
                      "type": "output_text",
                      "text": " How can I assist you today?",
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
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_741_891_428), response.CreatedAt);
        Assert.Null(response.FinishReason);

        Assert.Equal(2, response.Messages.Count);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.Equal("Hello!", response.Messages[0].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[1].Role);
        Assert.Equal(" How can I assist you today?", response.Messages[1].Text);

        Assert.NotNull(response.Usage);
        Assert.Equal(26, response.Usage.InputTokenCount);
        Assert.Equal(10, response.Usage.OutputTokenCount);
        Assert.Equal(36, response.Usage.TotalTokenCount);
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
