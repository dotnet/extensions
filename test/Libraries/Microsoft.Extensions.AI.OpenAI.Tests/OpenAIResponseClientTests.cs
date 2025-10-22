// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
                Assert.Null(updates[i].Role);

                var reasoning = Assert.IsType<TextReasoningContent>(updates[i].Contents.Single());
                Assert.NotNull(reasoning);
                Assert.NotNull(reasoning.Text);
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
                "temperature": 0.5,
                "top_p": 0.5,
                "previous_response_id": "resp_42",
                "model": "gpt-4o-mini",
                "max_output_tokens": 10,
                "text": {
                    "format": {
                        "type": "text"
                    }
                },
                "tools": [
                    {
                        "type": "function",
                        "name": "GetPersonAge",
                        "description": "Gets the age of the specified person.",
                        "parameters": {
                            "type": "object",
                            "required": [
                                "personName"
                            ],
                            "properties": {
                                "personName": {
                                    "description": "The person whose age is being requested",
                                    "type": "string"
                                }
                            },
                            "additionalProperties": false
                        },
                        "strict": null
                    },
                    {
                        "type": "function",
                        "name": "GetPersonAge",
                        "description": "Gets the age of the specified person.",
                        "parameters": {
                            "type": "object",
                            "required": [
                                "personName"
                            ],
                            "properties": {
                                "personName": {
                                    "description": "The person whose age is being requested",
                                    "type": "string"
                                }
                            },
                            "additionalProperties": false
                        },
                        "strict": null
                    }
                ],
                "tool_choice": "auto",
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
                "parallel_tool_calls": true
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
                openAIOptions.Tools.Add(tool.AsOpenAIResponseTool());
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

    [Theory]
    [InlineData("user")]
    [InlineData("tool")]
    public async Task McpToolCall_ApprovalRequired_NonStreaming(string role)
    {
        string input = """
            {
              "model": "gpt-4o-mini",
              "tools": [
                {
                  "type": "mcp",
                  "server_label": "deepwiki",
                  "server_url": "https://mcp.deepwiki.com/mcp"
                }
              ],
              "tool_choice": "auto",
              "input": [
                {
                  "type": "message",
                  "role": "user",
                  "content": [
                    {
                      "type": "input_text",
                      "text": "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository"
                    }
                  ]
                }
              ]
            }
            """;

        string output = """
            {
              "id": "resp_04e29d5bdd80bd9f0068e6b01f786081a29148febb92892aee",
              "object": "response",
              "created_at": 1759948831,
              "status": "completed",
              "background": false,
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": null,
              "max_tool_calls": null,
              "model": "gpt-4o-mini-2024-07-18",
              "output": [
                {
                  "id": "mcpr_04e29d5bdd80bd9f0068e6b022a9c081a2ae898104b7a75051",
                  "type": "mcp_approval_request",
                  "arguments": "{\"repoName\":\"dotnet/extensions\"}",
                  "name": "ask_question",
                  "server_label": "deepwiki"
                }
              ],
              "parallel_tool_calls": true,
              "previous_response_id": null,
              "prompt_cache_key": null,
              "reasoning": {
                "effort": null,
                "summary": null
              },
              "safety_identifier": null,
              "service_tier": "default",
              "store": true,
              "temperature": 1.0,
              "text": {
                "format": {
                  "type": "text"
                },
                "verbosity": "medium"
              },
              "tool_choice": "auto",
              "tools": [
                {
                  "type": "mcp",
                  "allowed_tools": null,
                  "headers": null,
                  "require_approval": "always",
                  "server_description": null,
                  "server_label": "deepwiki",
                  "server_url": "https://mcp.deepwiki.com/<redacted>"
                }
              ],
              "top_logprobs": 0,
              "top_p": 1.0,
              "truncation": "disabled",
              "usage": {
                "input_tokens": 193,
                "input_tokens_details": {
                  "cached_tokens": 0
                },
                "output_tokens": 23,
                "output_tokens_details": {
                  "reasoning_tokens": 0
                },
                "total_tokens": 216
              },
              "user": null,
              "metadata": {}
            }
            """;

        var chatOptions = new ChatOptions
        {
            Tools = [new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp"))]
        };
        McpServerToolApprovalRequestContent approvalRequest;

        using (VerbatimHttpHandler handler = new(input, output))
        using (HttpClient httpClient = new(handler))
        using (IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini"))
        {
            var response = await client.GetResponseAsync(
                "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository",
                chatOptions);

            approvalRequest = Assert.Single(response.Messages.SelectMany(m => m.Contents).OfType<McpServerToolApprovalRequestContent>());
            chatOptions.ConversationId = response.ConversationId;
        }

        input = $$"""
            {
                "previous_response_id": "resp_04e29d5bdd80bd9f0068e6b01f786081a29148febb92892aee",
                "model": "gpt-4o-mini",
                "tools": [
                    {
                        "type": "mcp",
                        "server_label": "deepwiki",
                        "server_url": "https://mcp.deepwiki.com/mcp"
                    }
                ],
                "tool_choice": "auto",
                "input": [
                    {
                        "type": "mcp_approval_response",
                        "approval_request_id": "mcpr_04e29d5bdd80bd9f0068e6b022a9c081a2ae898104b7a75051",
                        "approve": true
                    }
                ]
            }
            """;

        output = """
            {
              "id": "resp_06ee3b1962eeb8470068e6b21c377081a3a20dbf60eee7a736",
              "object": "response",
              "created_at": 1759949340,
              "status": "completed",
              "background": false,
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": null,
              "max_tool_calls": null,
              "model": "gpt-4o-mini-2024-07-18",
              "output": [
                {
                  "id": "mcp_06ee3b1962eeb8470068e6b21cbaa081a3b5aa2a6c989f4c6f",
                  "type": "mcp_call",
                  "status": "completed",
                  "approval_request_id": "mcpr_06ee3b1962eeb8470068e6b192985c81a383a16059ecd8230e",
                  "arguments": "{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}",
                  "error": null,
                  "name": "ask_question",
                  "output": "The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` within the `dotnet/extensions` repository.  This file provides an overview of the package, including installation instructions and usage examples for its core interfaces like `IChatClient` and `IEmbeddingGenerator`. \n\n## Path to README.md\n\nThe specific path to the `README.md` file for the `Microsoft.Extensions.AI.Abstractions` project is `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md`.  This path is also referenced in the `AI Extensions Framework` wiki page as a relevant source file. \n\n## Notes\n\nThe `Packaging.targets` file in the `eng/MSBuild` directory indicates that `README.md` files are included in packages when `IsPackable` and `IsShipping` properties are true.  This suggests that the `README.md` file located at `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` is intended to be part of the distributed NuGet package for `Microsoft.Extensions.AI.Abstractions`. \n\nWiki pages you might want to explore:\n- [AI Extensions Framework (dotnet/extensions)](/wiki/dotnet/extensions#3)\n- [Chat Completion (dotnet/extensions)](/wiki/dotnet/extensions#3.3)\n\nView this search on DeepWiki: https://deepwiki.com/search/what-is-the-path-to-the-readme_315595bd-9b39-4f04-9fa3-42dc778fa9f3\n",
                  "server_label": "deepwiki"
                },
                {
                  "id": "msg_06ee3b1962eeb8470068e6b226ab0081a39fccce9aa47aedbc",
                  "type": "message",
                  "status": "completed",
                  "content": [
                    {
                      "type": "output_text",
                      "annotations": [],
                      "logprobs": [],
                      "text": "The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the `Microsoft.Extensions.AI.Abstractions` package, including installation instructions and usage examples for its core interfaces like `IChatClient` and `IEmbeddingGenerator`."
                    }
                  ],
                  "role": "assistant"
                }
              ],
              "parallel_tool_calls": true,
              "previous_response_id": "resp_06ee3b1962eeb8470068e6b18e0db881a3bdfd255a60327cdc",
              "prompt_cache_key": null,
              "reasoning": {
                "effort": null,
                "summary": null
              },
              "safety_identifier": null,
              "service_tier": "default",
              "store": true,
              "temperature": 1.0,
              "text": {
                "format": {
                  "type": "text"
                },
                "verbosity": "medium"
              },
              "tool_choice": "auto",
              "tools": [
                {
                  "type": "mcp",
                  "allowed_tools": null,
                  "headers": null,
                  "require_approval": "always",
                  "server_description": null,
                  "server_label": "deepwiki",
                  "server_url": "https://mcp.deepwiki.com/<redacted>"
                }
              ],
              "top_logprobs": 0,
              "top_p": 1.0,
              "truncation": "disabled",
              "usage": {
                "input_tokens": 542,
                "input_tokens_details": {
                  "cached_tokens": 0
                },
                "output_tokens": 72,
                "output_tokens_details": {
                  "reasoning_tokens": 0
                },
                "total_tokens": 614
              },
              "user": null,
              "metadata": {}
            }
            """;

        using (VerbatimHttpHandler handler = new(input, output))
        using (HttpClient httpClient = new(handler))
        using (IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini"))
        {
            var response = await client.GetResponseAsync(
                new ChatMessage(new ChatRole(role), [approvalRequest.CreateResponse(true)]), chatOptions);

            Assert.NotNull(response);

            Assert.Equal("resp_06ee3b1962eeb8470068e6b21c377081a3a20dbf60eee7a736", response.ResponseId);
            Assert.Equal("resp_06ee3b1962eeb8470068e6b21c377081a3a20dbf60eee7a736", response.ConversationId);
            Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_759_949_340), response.CreatedAt);
            Assert.Null(response.FinishReason);

            var message = Assert.Single(response.Messages);
            Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
            Assert.Equal("The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the `Microsoft.Extensions.AI.Abstractions` package, including installation instructions and usage examples for its core interfaces like `IChatClient` and `IEmbeddingGenerator`.", response.Messages[0].Text);

            Assert.Equal(3, message.Contents.Count);

            var call = Assert.IsType<McpServerToolCallContent>(message.Contents[0]);
            Assert.Equal("mcp_06ee3b1962eeb8470068e6b21cbaa081a3b5aa2a6c989f4c6f", call.CallId);
            Assert.Equal("deepwiki", call.ServerName);
            Assert.Equal("ask_question", call.ToolName);
            Assert.NotNull(call.Arguments);
            Assert.Equal(2, call.Arguments.Count);
            Assert.Equal("dotnet/extensions", ((JsonElement)call.Arguments["repoName"]!).GetString());
            Assert.Equal("What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?", ((JsonElement)call.Arguments["question"]!).GetString());

            var result = Assert.IsType<McpServerToolResultContent>(message.Contents[1]);
            Assert.Equal("mcp_06ee3b1962eeb8470068e6b21cbaa081a3b5aa2a6c989f4c6f", result.CallId);
            Assert.NotNull(result.Output);
            Assert.StartsWith("The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at", Assert.IsType<TextContent>(Assert.Single(result.Output)).Text);

            Assert.NotNull(response.Usage);
            Assert.Equal(542, response.Usage.InputTokenCount);
            Assert.Equal(72, response.Usage.OutputTokenCount);
            Assert.Equal(614, response.Usage.TotalTokenCount);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task McpToolCall_ApprovalNotRequired_NonStreaming(bool rawTool)
    {
        const string Input = """
            {
                "model": "gpt-4o-mini",
                "tools": [
                    {
                        "type": "mcp",
                        "server_label": "deepwiki",
                        "server_url": "https://mcp.deepwiki.com/mcp",
                        "require_approval": "never"
                    }
                ],
                "tool_choice": "auto",
                "input": [
                    {
                        "type": "message",
                        "role": "user",
                        "content": [
                            {
                                "type": "input_text",
                                "text": "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository"
                            }
                        ]
                    }
                ]
            }
            """;

        const string Output = """
            {
                "id": "resp_68be416397ec81918c48ef286530b8140384f747588fc3f5",
                "object": "response",
                "created_at": 1757299043,
                "status": "completed",
                "background": false,
                "error": null,
                "incomplete_details": null,
                "instructions": null,
                "max_output_tokens": null,
                "max_tool_calls": null,
                "model": "gpt-4o-mini-2024-07-18",
                "output": [
                    {
                        "id": "mcpl_68be4163aa80819185e792abdcde71670384f747588fc3f5",
                        "type": "mcp_list_tools",
                        "server_label": "deepwiki",
                        "tools": [
                            {
                                "annotations": {
                                    "read_only": false
                                },
                                "description": "Get a list of documentation topics for a GitHub repository",
                                "input_schema": {
                                    "type": "object",
                                    "properties": {
                                        "repoName": {
                                            "type": "string",
                                            "description": "GitHub repository: owner/repo (e.g. \"facebook/react\")"
                                        }
                                    },
                                    "required": [
                                        "repoName"
                                    ],
                                    "additionalProperties": false,
                                    "$schema": "http://json-schema.org/draft-07/schema#"
                                },
                                "name": "read_wiki_structure"
                            },
                            {
                                "annotations": {
                                    "read_only": false
                                },
                                "description": "View documentation about a GitHub repository",
                                "input_schema": {
                                    "type": "object",
                                    "properties": {
                                        "repoName": {
                                            "type": "string",
                                            "description": "GitHub repository: owner/repo (e.g. \"facebook/react\")"
                                        }
                                    },
                                    "required": [
                                        "repoName"
                                    ],
                                    "additionalProperties": false,
                                    "$schema": "http://json-schema.org/draft-07/schema#"
                                },
                                "name": "read_wiki_contents"
                            },
                            {
                                "annotations": {
                                    "read_only": false
                                },
                                "description": "Ask any question about a GitHub repository",
                                "input_schema": {
                                    "type": "object",
                                    "properties": {
                                        "repoName": {
                                            "type": "string",
                                            "description": "GitHub repository: owner/repo (e.g. \"facebook/react\")"
                                        },
                                        "question": {
                                            "type": "string",
                                            "description": "The question to ask about the repository"
                                        }
                                    },
                                    "required": [
                                        "repoName",
                                        "question"
                                    ],
                                    "additionalProperties": false,
                                    "$schema": "http://json-schema.org/draft-07/schema#"
                                },
                                "name": "ask_question"
                            }
                        ]
                    },
                    {
                        "id": "mcp_68be4166acfc8191bc5e0a751eed358b0384f747588fc3f5",
                        "type": "mcp_call",
                        "approval_request_id": null,
                        "arguments": "{\"repoName\":\"dotnet/extensions\"}",
                        "error": null,
                        "name": "read_wiki_structure",
                        "output": "Available pages for dotnet/extensions:\n\n- 1 Overview\n- 2 Build System and CI/CD\n- 3 AI Extensions Framework\n  - 3.1 Core Abstractions\n  - 3.2 AI Function System\n  - 3.3 Chat Completion\n  - 3.4 Caching System\n  - 3.5 Evaluation and Reporting\n- 4 HTTP Resilience and Diagnostics\n  - 4.1 Standard Resilience\n  - 4.2 Hedging Strategies\n- 5 Telemetry and Compliance\n- 6 Testing Infrastructure\n  - 6.1 AI Service Integration Testing\n  - 6.2 Time Provider Testing",
                        "server_label": "deepwiki"
                    },
                    {
                        "id": "mcp_68be416900f88191837ae0718339a4ce0384f747588fc3f5",
                        "type": "mcp_call",
                        "approval_request_id": null,
                        "arguments": "{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}",
                        "error": null,
                        "name": "ask_question",
                        "output": "The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` within the `dotnet/extensions` repository.  This file provides an overview of the `Microsoft.Extensions.AI.Abstractions` package, including installation instructions and usage examples for its core interfaces like `IChatClient` and `IEmbeddingGenerator`. \n\n## Path to README.md\n\nThe specific path to the `README.md` file for the `Microsoft.Extensions.AI.Abstractions` project is `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md`.  This path is also referenced in the `AI Extensions Framework` wiki page as a relevant source file. \n\n## Notes\n\nThe `Packaging.targets` file in the `eng/MSBuild` directory indicates that `README.md` files are included in packages when `IsPackable` and `IsShipping` properties are true.  This suggests that the `README.md` file located at `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` is intended to be part of the distributed NuGet package for `Microsoft.Extensions.AI.Abstractions`. \n\nWiki pages you might want to explore:\n- [AI Extensions Framework (dotnet/extensions)](/wiki/dotnet/extensions#3)\n- [Chat Completion (dotnet/extensions)](/wiki/dotnet/extensions#3.3)\n\nView this search on DeepWiki: https://deepwiki.com/search/what-is-the-path-to-the-readme_315595bd-9b39-4f04-9fa3-42dc778fa9f3\n",
                        "server_label": "deepwiki"
                    },
                    {
                        "id": "msg_68be416fb43c819194a1d4ace2643a7e0384f747588fc3f5",
                        "type": "message",
                        "status": "completed",
                        "content": [
                            {
                                "type": "output_text",
                                "annotations": [],
                                "logprobs": [],
                                "text": "The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file includes an overview, installation instructions, and usage examples related to the package."
                            }
                        ],
                        "role": "assistant"
                    }
                ],
                "parallel_tool_calls": true,
                "previous_response_id": null,
                "prompt_cache_key": null,
                "reasoning": {
                    "effort": null,
                    "summary": null
                },
                "safety_identifier": null,
                "service_tier": "default",
                "store": true,
                "temperature": 1,
                "text": {
                    "format": {
                        "type": "text"
                    },
                    "verbosity": "medium"
                },
                "tool_choice": "auto",
                "tools": [
                    {
                        "type": "mcp",
                        "allowed_tools": null,
                        "headers": null,
                        "require_approval": "never",
                        "server_description": null,
                        "server_label": "deepwiki",
                        "server_url": "https://mcp.deepwiki.com/<redacted>"
                    }
                ],
                "top_logprobs": 0,
                "top_p": 1,
                "truncation": "disabled",
                "usage": {
                    "input_tokens": 1329,
                    "input_tokens_details": {
                        "cached_tokens": 0
                    },
                    "output_tokens": 123,
                    "output_tokens_details": {
                        "reasoning_tokens": 0
                    },
                    "total_tokens": 1452
                },
                "user": null,
                "metadata": {}
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        AITool mcpTool = rawTool ?
            ResponseTool.CreateMcpTool("deepwiki", serverUri: new("https://mcp.deepwiki.com/mcp"), toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval)).AsAITool() :
            new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp"))
            {
                ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
            };

        ChatOptions chatOptions = new()
        {
            Tools = [mcpTool],
        };

        var response = await client.GetResponseAsync("Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository", chatOptions);
        Assert.NotNull(response);

        Assert.Equal("resp_68be416397ec81918c48ef286530b8140384f747588fc3f5", response.ResponseId);
        Assert.Equal("resp_68be416397ec81918c48ef286530b8140384f747588fc3f5", response.ConversationId);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_757_299_043), response.CreatedAt);
        Assert.Null(response.FinishReason);

        var message = Assert.Single(response.Messages);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.Equal("The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file includes an overview, installation instructions, and usage examples related to the package.", response.Messages[0].Text);

        Assert.Equal(6, message.Contents.Count);

        var firstCall = Assert.IsType<McpServerToolCallContent>(message.Contents[1]);
        Assert.Equal("mcp_68be4166acfc8191bc5e0a751eed358b0384f747588fc3f5", firstCall.CallId);
        Assert.Equal("deepwiki", firstCall.ServerName);
        Assert.Equal("read_wiki_structure", firstCall.ToolName);
        Assert.NotNull(firstCall.Arguments);
        Assert.Single(firstCall.Arguments);
        Assert.Equal("dotnet/extensions", ((JsonElement)firstCall.Arguments["repoName"]!).GetString());

        var firstResult = Assert.IsType<McpServerToolResultContent>(message.Contents[2]);
        Assert.Equal("mcp_68be4166acfc8191bc5e0a751eed358b0384f747588fc3f5", firstResult.CallId);
        Assert.NotNull(firstResult.Output);
        Assert.StartsWith("Available pages for dotnet/extensions", Assert.IsType<TextContent>(Assert.Single(firstResult.Output)).Text);

        var secondCall = Assert.IsType<McpServerToolCallContent>(message.Contents[3]);
        Assert.Equal("mcp_68be416900f88191837ae0718339a4ce0384f747588fc3f5", secondCall.CallId);
        Assert.Equal("deepwiki", secondCall.ServerName);
        Assert.Equal("ask_question", secondCall.ToolName);
        Assert.NotNull(secondCall.Arguments);
        Assert.Equal("dotnet/extensions", ((JsonElement)secondCall.Arguments["repoName"]!).GetString());
        Assert.Equal("What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?", ((JsonElement)secondCall.Arguments["question"]!).GetString());

        var secondResult = Assert.IsType<McpServerToolResultContent>(message.Contents[4]);
        Assert.Equal("mcp_68be416900f88191837ae0718339a4ce0384f747588fc3f5", secondResult.CallId);
        Assert.NotNull(secondResult.Output);
        Assert.StartsWith("The `README.md` file for `Microsoft.Extensions.AI.Abstractions` is located at", Assert.IsType<TextContent>(Assert.Single(secondResult.Output)).Text);

        Assert.NotNull(response.Usage);
        Assert.Equal(1329, response.Usage.InputTokenCount);
        Assert.Equal(123, response.Usage.OutputTokenCount);
        Assert.Equal(1452, response.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task McpToolCall_ApprovalNotRequired_Streaming()
    {
        const string Input = """
            {
                "model": "gpt-4o-mini",
                "tools": [
                    {
                        "type": "mcp",
                        "server_label": "deepwiki",
                        "server_url": "https://mcp.deepwiki.com/mcp",
                        "require_approval": "never"
                    }
                ],
                "tool_choice": "auto",
                "input": [
                    {
                        "type": "message",
                        "role": "user",
                        "content": [
                            {
                                "type": "input_text",
                                "text": "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository"
                            }
                        ]
                    }
                ],
                "stream": true
            }
            """;

        const string Output = """
            event: response.created
            data: {"type":"response.created","sequence_number":0,"response":{"id":"resp_68be44fd7298819e82fd82c8516e970d03a2537be0e84a54","object":"response","created_at":1757299965,"status":"in_progress","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"gpt-4o-mini-2024-07-18","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":null,"summary":null},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[{"type":"mcp","allowed_tools":null,"headers":null,"require_approval":"never","server_description":null,"server_label":"deepwiki","server_url":"https://mcp.deepwiki.com/<redacted>"}],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.in_progress
            data: {"type":"response.in_progress","sequence_number":1,"response":{"id":"resp_68be44fd7298819e82fd82c8516e970d03a2537be0e84a54","object":"response","created_at":1757299965,"status":"in_progress","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"gpt-4o-mini-2024-07-18","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":null,"summary":null},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[{"type":"mcp","allowed_tools":null,"headers":null,"require_approval":"never","server_description":null,"server_label":"deepwiki","server_url":"https://mcp.deepwiki.com/<redacted>"}],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":2,"output_index":0,"item":{"id":"mcpl_68be44fd8f68819eba7a74a2f6d27a5a03a2537be0e84a54","type":"mcp_list_tools","server_label":"deepwiki","tools":[]}}

            event: response.mcp_list_tools.in_progress
            data: {"type":"response.mcp_list_tools.in_progress","sequence_number":3,"output_index":0,"item_id":"mcpl_68be44fd8f68819eba7a74a2f6d27a5a03a2537be0e84a54"}

            event: response.mcp_list_tools.completed
            data: {"type":"response.mcp_list_tools.completed","sequence_number":4,"output_index":0,"item_id":"mcpl_68be44fd8f68819eba7a74a2f6d27a5a03a2537be0e84a54"}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":5,"output_index":0,"item":{"id":"mcpl_68be44fd8f68819eba7a74a2f6d27a5a03a2537be0e84a54","type":"mcp_list_tools","server_label":"deepwiki","tools":[{"annotations":{"read_only":false},"description":"Get a list of documentation topics for a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"}},"required":["repoName"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"read_wiki_structure"},{"annotations":{"read_only":false},"description":"View documentation about a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"}},"required":["repoName"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"read_wiki_contents"},{"annotations":{"read_only":false},"description":"Ask any question about a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"},"question":{"type":"string","description":"The question to ask about the repository"}},"required":["repoName","question"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"ask_question"}]}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":6,"output_index":1,"item":{"id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"","error":null,"name":"read_wiki_structure","output":null,"server_label":"deepwiki"}}

            event: response.mcp_call.in_progress
            data: {"type":"response.mcp_call.in_progress","sequence_number":7,"output_index":1,"item_id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54"}

            event: response.mcp_call_arguments.delta
            data: {"type":"response.mcp_call_arguments.delta","sequence_number":8,"output_index":1,"item_id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54","delta":"{\"repoName\":\"dotnet/extensions\"}","obfuscation":""}

            event: response.mcp_call_arguments.done
            data: {"type":"response.mcp_call_arguments.done","sequence_number":9,"output_index":1,"item_id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54","arguments":"{\"repoName\":\"dotnet/extensions\"}"}

            event: response.mcp_call.completed
            data: {"type":"response.mcp_call.completed","sequence_number":10,"output_index":1,"item_id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54"}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":11,"output_index":1,"item":{"id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"{\"repoName\":\"dotnet/extensions\"}","error":null,"name":"read_wiki_structure","output":"Available pages for dotnet/extensions:\n\n- 1 Overview\n- 2 Build System and CI/CD\n- 3 AI Extensions Framework\n  - 3.1 Core Abstractions\n  - 3.2 AI Function System\n  - 3.3 Chat Completion\n  - 3.4 Caching System\n  - 3.5 Evaluation and Reporting\n- 4 HTTP Resilience and Diagnostics\n  - 4.1 Standard Resilience\n  - 4.2 Hedging Strategies\n- 5 Telemetry and Compliance\n- 6 Testing Infrastructure\n  - 6.1 AI Service Integration Testing\n  - 6.2 Time Provider Testing","server_label":"deepwiki"}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":12,"output_index":2,"item":{"id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"","error":null,"name":"ask_question","output":null,"server_label":"deepwiki"}}

            event: response.mcp_call.in_progress
            data: {"type":"response.mcp_call.in_progress","sequence_number":13,"output_index":2,"item_id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54"}

            event: response.mcp_call_arguments.delta
            data: {"type":"response.mcp_call_arguments.delta","sequence_number":14,"output_index":2,"item_id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54","delta":"{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}","obfuscation":"IT"}

            event: response.mcp_call_arguments.done
            data: {"type":"response.mcp_call_arguments.done","sequence_number":15,"output_index":2,"item_id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54","arguments":"{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}"}

            event: response.mcp_call.completed
            data: {"type":"response.mcp_call.completed","sequence_number":16,"output_index":2,"item_id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54"}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":17,"output_index":2,"item":{"id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}","error":null,"name":"ask_question","output":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` is `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` . This file provides an overview of the `Microsoft.Extensions.AI.Abstractions` library, including installation instructions and usage examples for its core components like `IChatClient` and `IEmbeddingGenerator`   .\n\n## README.md Content Overview\nThe `README.md` file for `Microsoft.Extensions.AI.Abstractions` details the purpose of the library, which is to provide abstractions for generative AI components . It includes instructions on how to install the NuGet package `Microsoft.Extensions.AI.Abstractions` .\n\nThe document also provides usage examples for the `IChatClient` interface, which defines methods for interacting with AI services that offer \"chat\" capabilities . This includes examples for requesting both complete and streaming chat responses  .\n\nFurthermore, the `README.md` explains the `IEmbeddingGenerator` interface, which is used for generating vector embeddings from input values . It demonstrates how to use `GenerateAsync` to create embeddings . The file also discusses how both `IChatClient` and `IEmbeddingGenerator` implementations can be layered to create pipelines of functionality, incorporating features like caching and telemetry  .\n\nNotes:\nThe user's query specifically asked for the path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions`. The provided codebase context, particularly the wiki page for \"AI Extensions Framework\", directly lists this file as a relevant source file . The content of the `README.md` file itself further confirms its relevance to the `Microsoft.Extensions.AI.Abstractions` library.\n\nWiki pages you might want to explore:\n- [AI Extensions Framework (dotnet/extensions)](/wiki/dotnet/extensions#3)\n- [Chat Completion (dotnet/extensions)](/wiki/dotnet/extensions#3.3)\n\nView this search on DeepWiki: https://deepwiki.com/search/what-is-the-path-to-the-readme_bb6bee43-3136-4b21-bc5d-02ca1611d857\n","server_label":"deepwiki"}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":18,"output_index":3,"item":{"id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","type":"message","status":"in_progress","content":[],"role":"assistant"}}

            event: response.content_part.added
            data: {"type":"response.content_part.added","sequence_number":19,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"part":{"type":"output_text","annotations":[],"logprobs":[],"text":""}}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":20,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"The","logprobs":[],"obfuscation":"a5sNdjeWpJXIK"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":21,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" path","logprobs":[],"obfuscation":"2oWbALsHrtv"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":22,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" to","logprobs":[],"obfuscation":"K8lRBCaiusvjP"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":23,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" the","logprobs":[],"obfuscation":"LP7Xp4jDWA5w"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":24,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" `","logprobs":[],"obfuscation":"2rUNEj0h3wLlee"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":25,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"README","logprobs":[],"obfuscation":"PSbOrCj8y6"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":26,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".md","logprobs":[],"obfuscation":"Do0BCY4kJ6wQW"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":27,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"`","logprobs":[],"obfuscation":"3fTPkjHu1Oq83DT"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":28,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" file","logprobs":[],"obfuscation":"CI9PXx3sH06"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":29,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" for","logprobs":[],"obfuscation":"fJuaoSPsMge8"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":30,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" `","logprobs":[],"obfuscation":"O1h4Q0T72OM4e7"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":31,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"Microsoft","logprobs":[],"obfuscation":"E2YPgfE"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":32,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".Extensions","logprobs":[],"obfuscation":"vfVX8"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":33,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".A","logprobs":[],"obfuscation":"EwDmSMHqymBRl1"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":34,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"I","logprobs":[],"obfuscation":"QQfjze1z7QhvcJE"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":35,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".A","logprobs":[],"obfuscation":"7fLbFXKbxOMkBi"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":36,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"bst","logprobs":[],"obfuscation":"3p1svK7Jd1N7C"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":37,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"ractions","logprobs":[],"obfuscation":"Cl2xCwTC"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":38,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"`","logprobs":[],"obfuscation":"ObDOKE72QOlXSx9"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":39,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" in","logprobs":[],"obfuscation":"FJwPbDYgh4XjL"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":40,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" the","logprobs":[],"obfuscation":"e8cV5qt7hEsz"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":41,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" `","logprobs":[],"obfuscation":"Hf8ZQDFLfImh3e"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":42,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"dot","logprobs":[],"obfuscation":"0lh2vLiYye2JI"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":43,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"net","logprobs":[],"obfuscation":"g5fzb2qtk4Piz"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":44,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"/extensions","logprobs":[],"obfuscation":"egpos"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":45,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"`","logprobs":[],"obfuscation":"gXw3bKveEVIKXux"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":46,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" repository","logprobs":[],"obfuscation":"rqhlC"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":47,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" is","logprobs":[],"obfuscation":"YZq9zsRja0g2M"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":48,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":":\n\n","logprobs":[],"obfuscation":"mhDAmaHJUvLGl"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":49,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"``","logprobs":[],"obfuscation":"3XmO5YTsWjzHHf"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":50,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"`\n","logprobs":[],"obfuscation":"4fmXZmdkPxNn8K"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":51,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"src","logprobs":[],"obfuscation":"ifGf4yLEg5pMZ"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":52,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"/L","logprobs":[],"obfuscation":"C1k1toBElpgxyW"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":53,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"ibraries","logprobs":[],"obfuscation":"fdOTYTyp"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":54,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"/M","logprobs":[],"obfuscation":"DyscJIQYaPJugC"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":55,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"icrosoft","logprobs":[],"obfuscation":"PQxU7muP"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":56,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".Extensions","logprobs":[],"obfuscation":"RCJB8"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":57,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".A","logprobs":[],"obfuscation":"i92CWxnAkwS4C9"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":58,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"I","logprobs":[],"obfuscation":"qfH8wVJN74vCfBM"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":59,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".A","logprobs":[],"obfuscation":"LcuBP89lZVCCH9"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":60,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"bst","logprobs":[],"obfuscation":"I8rKDbKN0zylv"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":61,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"ractions","logprobs":[],"obfuscation":"tOgiCPs5"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":62,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"/","logprobs":[],"obfuscation":"jgJjLruTbFJGDhU"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":63,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"README","logprobs":[],"obfuscation":"D5VSEFNde7"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":64,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".md","logprobs":[],"obfuscation":"7ZGJO5sZOTPBs"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":65,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"\n","logprobs":[],"obfuscation":"7Sv80haKTTwfEWj"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":66,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"``","logprobs":[],"obfuscation":"m1JSvZ8rrpJnH5"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":67,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"`\n\n","logprobs":[],"obfuscation":"U93PMKtCB5Pb5"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":68,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"This","logprobs":[],"obfuscation":"f5veTGedo9nM"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":69,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" file","logprobs":[],"obfuscation":"oEBwvP5FnPK"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":70,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" provides","logprobs":[],"obfuscation":"IVNCYwr"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":71,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" an","logprobs":[],"obfuscation":"3x6WquURIJ3ld"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":72,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" overview","logprobs":[],"obfuscation":"VR9yeiD"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":73,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" of","logprobs":[],"obfuscation":"z46dC1o2FC8Rs"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":74,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" the","logprobs":[],"obfuscation":"YfZGabvmgyoI"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":75,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" library","logprobs":[],"obfuscation":"TamElgEp"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":76,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":",","logprobs":[],"obfuscation":"VfVfqbnHAfsJyJn"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":77,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" installation","logprobs":[],"obfuscation":"CGR"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":78,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" instructions","logprobs":[],"obfuscation":"xst"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":79,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":",","logprobs":[],"obfuscation":"3u5wqRA2RXh2QP8"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":80,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" and","logprobs":[],"obfuscation":"tD4WZmOhepzQ"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":81,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" usage","logprobs":[],"obfuscation":"SadOK826mZ"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":82,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" examples","logprobs":[],"obfuscation":"5VpLKav"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":83,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" for","logprobs":[],"obfuscation":"xPvtjDSUic9E"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":84,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" its","logprobs":[],"obfuscation":"6duK61DX14vx"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":85,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" core","logprobs":[],"obfuscation":"Cz8trPLsCWu"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":86,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" components","logprobs":[],"obfuscation":"Gexuy"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":87,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":".","logprobs":[],"obfuscation":"HVeWkHoX1cc6hVh"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":88,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" If","logprobs":[],"obfuscation":"G1TOxxwvSEq4L"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":89,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" you","logprobs":[],"obfuscation":"xQlKeOixd1hv"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":90,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" have","logprobs":[],"obfuscation":"bX6P0qgFPnR"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":91,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" any","logprobs":[],"obfuscation":"KxH8EiMzXa1N"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":92,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" more","logprobs":[],"obfuscation":"kA0kxRPPqru"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":93,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" questions","logprobs":[],"obfuscation":"9HRCyD"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":94,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" about","logprobs":[],"obfuscation":"yYFZhtsSfc"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":95,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" it","logprobs":[],"obfuscation":"zpyEAwPWl8Ozh"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":96,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":",","logprobs":[],"obfuscation":"ivjn00lbmzDHiFU"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":97,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" feel","logprobs":[],"obfuscation":"O2edXDmkBqt"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":98,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" free","logprobs":[],"obfuscation":"MlpWh7p0P1F"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":99,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" to","logprobs":[],"obfuscation":"uMNfozGkKe6xW"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":100,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":" ask","logprobs":[],"obfuscation":"6rMOxwXhR8RY"}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":101,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"delta":"!","logprobs":[],"obfuscation":"QPZMdhS0e5vYuRl"}

            event: response.output_text.done
            data: {"type":"response.output_text.done","sequence_number":102,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"text":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` in the `dotnet/extensions` repository is:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the library, installation instructions, and usage examples for its core components. If you have any more questions about it, feel free to ask!","logprobs":[]}

            event: response.content_part.done
            data: {"type":"response.content_part.done","sequence_number":103,"item_id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","output_index":3,"content_index":0,"part":{"type":"output_text","annotations":[],"logprobs":[],"text":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` in the `dotnet/extensions` repository is:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the library, installation instructions, and usage examples for its core components. If you have any more questions about it, feel free to ask!"}}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":104,"output_index":3,"item":{"id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","type":"message","status":"completed","content":[{"type":"output_text","annotations":[],"logprobs":[],"text":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` in the `dotnet/extensions` repository is:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the library, installation instructions, and usage examples for its core components. If you have any more questions about it, feel free to ask!"}],"role":"assistant"}}

            event: response.completed
            data: {"type":"response.completed","sequence_number":105,"response":{"id":"resp_68be44fd7298819e82fd82c8516e970d03a2537be0e84a54","object":"response","created_at":1757299965,"status":"completed","background":false,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"gpt-4o-mini-2024-07-18","output":[{"id":"mcpl_68be44fd8f68819eba7a74a2f6d27a5a03a2537be0e84a54","type":"mcp_list_tools","server_label":"deepwiki","tools":[{"annotations":{"read_only":false},"description":"Get a list of documentation topics for a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"}},"required":["repoName"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"read_wiki_structure"},{"annotations":{"read_only":false},"description":"View documentation about a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"}},"required":["repoName"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"read_wiki_contents"},{"annotations":{"read_only":false},"description":"Ask any question about a GitHub repository","input_schema":{"type":"object","properties":{"repoName":{"type":"string","description":"GitHub repository: owner/repo (e.g. \"facebook/react\")"},"question":{"type":"string","description":"The question to ask about the repository"}},"required":["repoName","question"],"additionalProperties":false,"$schema":"http://json-schema.org/draft-07/schema#"},"name":"ask_question"}]},{"id":"mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"{\"repoName\":\"dotnet/extensions\"}","error":null,"name":"read_wiki_structure","output":"Available pages for dotnet/extensions:\n\n- 1 Overview\n- 2 Build System and CI/CD\n- 3 AI Extensions Framework\n  - 3.1 Core Abstractions\n  - 3.2 AI Function System\n  - 3.3 Chat Completion\n  - 3.4 Caching System\n  - 3.5 Evaluation and Reporting\n- 4 HTTP Resilience and Diagnostics\n  - 4.1 Standard Resilience\n  - 4.2 Hedging Strategies\n- 5 Telemetry and Compliance\n- 6 Testing Infrastructure\n  - 6.1 AI Service Integration Testing\n  - 6.2 Time Provider Testing","server_label":"deepwiki"},{"id":"mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54","type":"mcp_call","approval_request_id":null,"arguments":"{\"repoName\":\"dotnet/extensions\",\"question\":\"What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?\"}","error":null,"name":"ask_question","output":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` is `src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md` . This file provides an overview of the `Microsoft.Extensions.AI.Abstractions` library, including installation instructions and usage examples for its core components like `IChatClient` and `IEmbeddingGenerator`   .\n\n## README.md Content Overview\nThe `README.md` file for `Microsoft.Extensions.AI.Abstractions` details the purpose of the library, which is to provide abstractions for generative AI components . It includes instructions on how to install the NuGet package `Microsoft.Extensions.AI.Abstractions` .\n\nThe document also provides usage examples for the `IChatClient` interface, which defines methods for interacting with AI services that offer \"chat\" capabilities . This includes examples for requesting both complete and streaming chat responses  .\n\nFurthermore, the `README.md` explains the `IEmbeddingGenerator` interface, which is used for generating vector embeddings from input values . It demonstrates how to use `GenerateAsync` to create embeddings . The file also discusses how both `IChatClient` and `IEmbeddingGenerator` implementations can be layered to create pipelines of functionality, incorporating features like caching and telemetry  .\n\nNotes:\nThe user's query specifically asked for the path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions`. The provided codebase context, particularly the wiki page for \"AI Extensions Framework\", directly lists this file as a relevant source file . The content of the `README.md` file itself further confirms its relevance to the `Microsoft.Extensions.AI.Abstractions` library.\n\nWiki pages you might want to explore:\n- [AI Extensions Framework (dotnet/extensions)](/wiki/dotnet/extensions#3)\n- [Chat Completion (dotnet/extensions)](/wiki/dotnet/extensions#3.3)\n\nView this search on DeepWiki: https://deepwiki.com/search/what-is-the-path-to-the-readme_bb6bee43-3136-4b21-bc5d-02ca1611d857\n","server_label":"deepwiki"},{"id":"msg_68be450c39e8819eb9bf6fcb9fd16ecb03a2537be0e84a54","type":"message","status":"completed","content":[{"type":"output_text","annotations":[],"logprobs":[],"text":"The path to the `README.md` file for `Microsoft.Extensions.AI.Abstractions` in the `dotnet/extensions` repository is:\n\n```\nsrc/Libraries/Microsoft.Extensions.AI.Abstractions/README.md\n```\n\nThis file provides an overview of the library, installation instructions, and usage examples for its core components. If you have any more questions about it, feel free to ask!"}],"role":"assistant"}],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":null,"summary":null},"safety_identifier":null,"service_tier":"default","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[{"type":"mcp","allowed_tools":null,"headers":null,"require_approval":"never","server_description":null,"server_label":"deepwiki","server_url":"https://mcp.deepwiki.com/<redacted>"}],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":{"input_tokens":1420,"input_tokens_details":{"cached_tokens":0},"output_tokens":149,"output_tokens_details":{"reasoning_tokens":0},"total_tokens":1569},"user":null,"metadata":{}}}
            
            
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        ChatOptions chatOptions = new()
        {
            Tools = [new HostedMcpServerTool("deepwiki", new Uri("https://mcp.deepwiki.com/mcp"))
                {
                    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
                }
            ],
        };

        var response = await client.GetStreamingResponseAsync("Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository", chatOptions)
            .ToChatResponseAsync();
        Assert.NotNull(response);

        Assert.Equal("resp_68be44fd7298819e82fd82c8516e970d03a2537be0e84a54", response.ResponseId);
        Assert.Equal("resp_68be44fd7298819e82fd82c8516e970d03a2537be0e84a54", response.ConversationId);
        Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_757_299_965), response.CreatedAt);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        var message = Assert.Single(response.Messages);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.StartsWith("The path to the `README.md` file", response.Messages[0].Text);

        Assert.Equal(6, message.Contents.Count);

        var firstCall = Assert.IsType<McpServerToolCallContent>(message.Contents[1]);
        Assert.Equal("mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54", firstCall.CallId);
        Assert.Equal("deepwiki", firstCall.ServerName);
        Assert.Equal("read_wiki_structure", firstCall.ToolName);
        Assert.NotNull(firstCall.Arguments);
        Assert.Single(firstCall.Arguments);
        Assert.Equal("dotnet/extensions", ((JsonElement)firstCall.Arguments["repoName"]!).GetString());

        var firstResult = Assert.IsType<McpServerToolResultContent>(message.Contents[2]);
        Assert.Equal("mcp_68be4503d45c819e89cb574361c8eba003a2537be0e84a54", firstResult.CallId);
        Assert.NotNull(firstResult.Output);
        Assert.StartsWith("Available pages for dotnet/extensions", Assert.IsType<TextContent>(Assert.Single(firstResult.Output)).Text);

        var secondCall = Assert.IsType<McpServerToolCallContent>(message.Contents[3]);
        Assert.Equal("mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54", secondCall.CallId);
        Assert.Equal("deepwiki", secondCall.ServerName);
        Assert.Equal("ask_question", secondCall.ToolName);
        Assert.NotNull(secondCall.Arguments);
        Assert.Equal("dotnet/extensions", ((JsonElement)secondCall.Arguments["repoName"]!).GetString());
        Assert.Equal("What is the path to the README.md file for Microsoft.Extensions.AI.Abstractions?", ((JsonElement)secondCall.Arguments["question"]!).GetString());

        var secondResult = Assert.IsType<McpServerToolResultContent>(message.Contents[4]);
        Assert.Equal("mcp_68be4505f134819e806c002f27cce0c303a2537be0e84a54", secondResult.CallId);
        Assert.NotNull(secondResult.Output);
        Assert.StartsWith("The path to the `README.md` file", Assert.IsType<TextContent>(Assert.Single(secondResult.Output)).Text);

        Assert.NotNull(response.Usage);
        Assert.Equal(1420, response.Usage.InputTokenCount);
        Assert.Equal(149, response.Usage.OutputTokenCount);
        Assert.Equal(1569, response.Usage.TotalTokenCount);
    }

    [Fact]
    public async Task GetResponseAsync_BackgroundResponses_FirstCall()
    {
        const string Input = """
            {
                "temperature":0.5,
                "model":"gpt-4o-mini",
                "background":true,
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
              "id": "resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed7",
              "object": "response",
              "created_at": 1758712522,
              "status": "queued",
              "background": true,
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": null,
              "model": "gpt-4o-mini-2024-07-18",
              "output": [],
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
              "usage": null,
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
            AllowBackgroundResponses = true,
        });
        Assert.NotNull(response);

        Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed7", response.ResponseId);
        Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed7", response.ConversationId);
        Assert.Empty(response.Messages);

        Assert.NotNull(response.ContinuationToken);
        var responsesContinuationToken = TestOpenAIResponsesContinuationToken.FromToken(response.ContinuationToken);
        Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed7", responsesContinuationToken.ResponseId);
        Assert.Null(responsesContinuationToken.SequenceNumber);
    }

    [Theory]
    [InlineData(ResponseStatus.Queued)]
    [InlineData(ResponseStatus.InProgress)]
    [InlineData(ResponseStatus.Completed)]
    [InlineData(ResponseStatus.Cancelled)]
    [InlineData(ResponseStatus.Failed)]
    [InlineData(ResponseStatus.Incomplete)]
    public async Task GetResponseAsync_BackgroundResponses_PollingCall(ResponseStatus expectedStatus)
    {
        var expectedInput = new HttpHandlerExpectedInput
        {
            Uri = new Uri("https://api.openai.com/v1/responses/resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8"),
            Method = HttpMethod.Get,
        };

        string output = $$""""
            {
              "id": "resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8",
              "object": "response",
              "created_at": 1758712522,
              "status": "{{ResponseStatusToRequestValue(expectedStatus)}}",
              "background": true,
              "error": null,
              "incomplete_details": null,
              "instructions": null,
              "max_output_tokens": null,
              "model": "gpt-4o-mini-2024-07-18",
              "output": {{(expectedStatus is (ResponseStatus.Queued or ResponseStatus.InProgress)
                ? "[]"
                : """
                    [{
                      "type": "message",
                      "id": "msg_67d32764fcdc8191bcf2e444d4088804058a5e08c46a181d",
                      "status": "completed",
                      "role": "assistant",
                      "content": [
                        {
                          "type": "output_text",
                          "text": "The background response result.",
                          "annotations": []
                        }
                      ]
                    }]
                    """
              )}},
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
              "usage": null,
              "user": null,
              "metadata": {}
            }
            """";

        using VerbatimHttpHandler handler = new(expectedInput, output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        var continuationToken = new TestOpenAIResponsesContinuationToken("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8");

        var response = await client.GetResponseAsync([], new()
        {
            ContinuationToken = continuationToken,
            AllowBackgroundResponses = true,
        });
        Assert.NotNull(response);

        Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8", response.ResponseId);
        Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8", response.ConversationId);

        switch (expectedStatus)
        {
            case ResponseStatus.Queued:
            case ResponseStatus.InProgress:
            {
                Assert.NotNull(response.ContinuationToken);

                var responsesContinuationToken = TestOpenAIResponsesContinuationToken.FromToken(response.ContinuationToken);
                Assert.Equal("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8", responsesContinuationToken.ResponseId);
                Assert.Null(responsesContinuationToken.SequenceNumber);

                Assert.Empty(response.Messages);
                break;
            }

            case ResponseStatus.Completed:
            case ResponseStatus.Cancelled:
            case ResponseStatus.Failed:
            case ResponseStatus.Incomplete:
            {
                Assert.Null(response.ContinuationToken);

                Assert.Equal("The background response result.", response.Text);
                Assert.Single(response.Messages.Single().Contents);
                Assert.Equal(ChatRole.Assistant, response.Messages.Single().Role);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(expectedStatus), expectedStatus, null);
        }
    }

    [Fact]
    public async Task GetResponseAsync_BackgroundResponses_PollingCall_WithMessages()
    {
        using VerbatimHttpHandler handler = new(string.Empty, string.Empty);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        var options = new ChatOptions
        {
            ContinuationToken = new TestOpenAIResponsesContinuationToken("resp_68d3d2c9ef7c8195863e4e2b2ec226a205007262ecbbfed8"),
            AllowBackgroundResponses = true,
        };

        // A try to update a background response with new messages should fail.
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await client.GetResponseAsync("Please book hotel as well", options);
        });
    }

    [Fact]
    public async Task GetStreamingResponseAsync_BackgroundResponses()
    {
        const string Input = """
            {
              "model": "gpt-4o-2024-08-06",
              "background": true,
              "input":[{
                "type":"message",
                "role":"user",
                "content":[{
                  "type":"input_text",
                  "text":"hello"
                }]
              }],
              
              "stream": true
            }
            """;

        const string Output = """
            event: response.created
            data: {"type":"response.created","sequence_number":0,"response":{"id":"resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559","object":"response","created_at":1758724519,"status":"queued","background":true,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"gpt-4o-2024-08-06","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":null,"summary":null},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.queued
            data: {"type":"response.queued","sequence_number":1,"response":{"id":"resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559","object":"response","created_at":1758724519,"status":"queued","background":true,"error":null,"incomplete_details":null,"instructions":null,"max_output_tokens":null,"max_tool_calls":null,"model":"gpt-4o-2024-08-06","output":[],"parallel_tool_calls":true,"previous_response_id":null,"prompt_cache_key":null,"reasoning":{"effort":null,"summary":null},"safety_identifier":null,"service_tier":"auto","store":true,"temperature":1.0,"text":{"format":{"type":"text"},"verbosity":"medium"},"tool_choice":"auto","tools":[],"top_logprobs":0,"top_p":1.0,"truncation":"disabled","usage":null,"user":null,"metadata":{}}}

            event: response.in_progress
            data: {"type":"response.in_progress","sequence_number":2,"response":{"truncation":"disabled","id":"resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559","tool_choice":"auto","temperature":1.0,"top_p":1.0,"status":"in_progress","top_logprobs":0,"usage":null,"object":"response","created_at":1758724519,"prompt_cache_key":null,"text":{"format":{"type":"text"},"verbosity":"medium"},"incomplete_details":null,"model":"gpt-4o-2024-08-06","previous_response_id":null,"safety_identifier":null,"metadata":{},"store":true,"output":[],"parallel_tool_calls":true,"error":null,"background":true,"instructions":null,"service_tier":"auto","max_tool_calls":null,"max_output_tokens":null,"tools":[],"user":null,"reasoning":{"effort":null,"summary":null}}}

            event: response.output_item.added
            data: {"type":"response.output_item.added","sequence_number":3,"item":{"id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content":[],"role":"assistant","status":"in_progress","type":"message"},"output_index":0}

            event: response.content_part.added
            data: {"type":"response.content_part.added","sequence_number":4,"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"part":{"text":"","logprobs":[],"type":"output_text","annotations":[]},"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":5,"delta":"Hello","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":6,"delta":"!","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":7,"delta":" How","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":8,"delta":" can","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":9,"delta":" I","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":10,"delta":" assist","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":11,"delta":" you","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":12,"delta":" today","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":13,"delta":"?","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.output_text.done
            data: {"type":"response.output_text.done","sequence_number":14,"text":"Hello! How can I assist you today?","logprobs":[],"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"output_index":0}

            event: response.content_part.done
            data: {"type":"response.content_part.done","sequence_number":15,"item_id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content_index":0,"part":{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]},"output_index":0}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":16,"item":{"id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content":[{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]}],"role":"assistant","status":"completed","type":"message"},"output_index":0}

            event: response.completed
            data: {"type":"response.completed","sequence_number":17,"response":{"truncation":"disabled","id":"resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559","tool_choice":"auto","temperature":1.0,"top_p":1.0,"status":"completed","top_logprobs":0,"usage":{"total_tokens":18,"input_tokens_details":{"cached_tokens":0},"output_tokens_details":{"reasoning_tokens":0},"output_tokens":10,"input_tokens":8},"object":"response","created_at":1758724519,"prompt_cache_key":null,"text":{"format":{"type":"text"},"verbosity":"medium"},"incomplete_details":null,"model":"gpt-4o-2024-08-06","previous_response_id":null,"safety_identifier":null,"metadata":{},"store":true,"output":[{"id":"msg_68d401aa78d481a2ab30776a79c691a6073420ed59d5f559","content":[{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]}],"role":"assistant","status":"completed","type":"message"}],"parallel_tool_calls":true,"error":null,"background":true,"instructions":null,"service_tier":"default","max_tool_calls":null,"max_output_tokens":null,"tools":[],"user":null,"reasoning":{"effort":null,"summary":null}}}

            
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-2024-08-06");

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync("hello", new()
        {
            AllowBackgroundResponses = true,
        }))
        {
            updates.Add(update);
        }

        Assert.Equal("Hello! How can I assist you today?", string.Concat(updates.Select(u => u.Text)));
        Assert.Equal(18, updates.Count);

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_758_724_519);

        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559", updates[i].ResponseId);
            Assert.Equal("resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559", updates[i].ConversationId);
            Assert.Equal(createdAt, updates[i].CreatedAt);
            Assert.Equal("gpt-4o-2024-08-06", updates[i].ModelId);
            Assert.Null(updates[i].AdditionalProperties);

            if (i < updates.Count - 1)
            {
                Assert.NotNull(updates[i].ContinuationToken);
                var responsesContinuationToken = TestOpenAIResponsesContinuationToken.FromToken(updates[i].ContinuationToken!);
                Assert.Equal("resp_68d401a7b36c81a288600e95a5a119d4073420ed59d5f559", responsesContinuationToken.ResponseId);
                Assert.Equal(i, responsesContinuationToken.SequenceNumber);
                Assert.Null(updates[i].FinishReason);
            }
            else
            {
                Assert.Null(updates[i].ContinuationToken);
                Assert.Equal(ChatFinishReason.Stop, updates[i].FinishReason);
            }

            Assert.Equal((i >= 5 && i <= 13) || i == 17 ? 1 : 0, updates[i].Contents.Count);
        }
    }

    [Fact]
    public async Task GetStreamingResponseAsync_BackgroundResponses_StreamResumption()
    {
        var expectedInput = new HttpHandlerExpectedInput
        {
            Uri = new Uri("https://api.openai.com/v1/responses/resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b?stream=true&starting_after=9"),
            Method = HttpMethod.Get,
        };

        const string Output = """
            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":10,"delta":" assist","logprobs":[],"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":11,"delta":" you","logprobs":[],"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":12,"delta":" today","logprobs":[],"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"output_index":0}

            event: response.output_text.delta
            data: {"type":"response.output_text.delta","sequence_number":13,"delta":"?","logprobs":[],"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"output_index":0}

            event: response.output_text.done
            data: {"type":"response.output_text.done","sequence_number":14,"text":"Hello! How can I assist you today?","logprobs":[],"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"output_index":0}

            event: response.content_part.done
            data: {"type":"response.content_part.done","sequence_number":15,"item_id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content_index":0,"part":{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]},"output_index":0}

            event: response.output_item.done
            data: {"type":"response.output_item.done","sequence_number":16,"item":{"id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content":[{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]}],"role":"assistant","status":"completed","type":"message"},"output_index":0}

            event: response.completed
            data: {"type":"response.completed","sequence_number":17,"response":{"truncation":"disabled","id":"resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b","tool_choice":"auto","temperature":1.0,"top_p":1.0,"status":"completed","top_logprobs":0,"usage":{"total_tokens":18,"input_tokens_details":{"cached_tokens":0},"output_tokens_details":{"reasoning_tokens":0},"output_tokens":10,"input_tokens":8},"object":"response","created_at":1758727622,"prompt_cache_key":null,"text":{"format":{"type":"text"},"verbosity":"medium"},"incomplete_details":null,"model":"gpt-4o-2024-08-06","previous_response_id":null,"safety_identifier":null,"metadata":{},"store":true,"output":[{"id":"msg_68d40dcb2d34819c88f5d6a8ca7b0308029e611c3cc4a34b","content":[{"text":"Hello! How can I assist you today?","logprobs":[],"type":"output_text","annotations":[]}],"role":"assistant","status":"completed","type":"message"}],"parallel_tool_calls":true,"error":null,"background":true,"instructions":null,"service_tier":"default","max_tool_calls":null,"max_output_tokens":null,"tools":[],"user":null,"reasoning":{"effort":null,"summary":null}}}

            
            """;

        using VerbatimHttpHandler handler = new(expectedInput, Output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-2024-08-06");

        // Emulating resumption of the stream after receiving the first 9 updates that provided the text "Hello! How can I"
        var continuationToken = new TestOpenAIResponsesContinuationToken("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b")
        {
            SequenceNumber = 9
        };

        var chatOptions = new ChatOptions
        {
            AllowBackgroundResponses = true,
            ContinuationToken = continuationToken,
        };

        List<ChatResponseUpdate> updates = [];
        await foreach (var update in client.GetStreamingResponseAsync([], chatOptions))
        {
            updates.Add(update);
        }

        // Receiving the remaining updates to complete the response "Hello! How can I assist you today?"
        Assert.Equal(" assist you today?", string.Concat(updates.Select(u => u.Text)));
        Assert.Equal(8, updates.Count);

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(1_758_727_622);

        for (int i = 0; i < updates.Count; i++)
        {
            Assert.Equal("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b", updates[i].ResponseId);
            Assert.Equal("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b", updates[i].ConversationId);

            var sequenceNumber = i + 10;

            if (sequenceNumber is (>= 10 and <= 13))
            {
                // Text deltas
                Assert.NotNull(updates[i].ContinuationToken);
                var responsesContinuationToken = TestOpenAIResponsesContinuationToken.FromToken(updates[i].ContinuationToken!);
                Assert.Equal("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b", responsesContinuationToken.ResponseId);
                Assert.Equal(sequenceNumber, responsesContinuationToken.SequenceNumber);

                Assert.Single(updates[i].Contents);
            }
            else if (sequenceNumber is (>= 14 and <= 16))
            {
                // Response Complete and Assistant message updates
                Assert.NotNull(updates[i].ContinuationToken);
                var responsesContinuationToken = TestOpenAIResponsesContinuationToken.FromToken(updates[i].ContinuationToken!);
                Assert.Equal("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b", responsesContinuationToken.ResponseId);
                Assert.Equal(sequenceNumber, responsesContinuationToken.SequenceNumber);

                Assert.Empty(updates[i].Contents);
            }
            else
            {
                // The last update with the response completion
                Assert.Null(updates[i].ContinuationToken);
                Assert.Single(updates[i].Contents);
            }
        }
    }

    [Fact]
    public async Task GetStreamingResponseAsync_BackgroundResponses_StreamResumption_WithMessages()
    {
        using VerbatimHttpHandler handler = new(string.Empty, string.Empty);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-2024-08-06");

        // Emulating resumption of the stream after receiving the first 9 updates that provided the text "Hello! How can I"
        var chatOptions = new ChatOptions
        {
            AllowBackgroundResponses = true,
            ContinuationToken = new TestOpenAIResponsesContinuationToken("resp_68d40dc671a0819cb0ee920078333451029e611c3cc4a34b")
            {
                SequenceNumber = 9
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync("Please book a hotel for me", chatOptions))
#pragma warning disable S108 // Nested blocks of code should not be left empty
            {
            }
#pragma warning restore S108 // Nested blocks of code should not be left empty
        });
    }

    [Fact]
    public async Task RequestHeaders_UserAgent_ContainsMEAI()
    {
        using var handler = new ThrowUserAgentExceptionHandler();
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateResponseClient(httpClient, "gpt-4o-mini");

        InvalidOperationException e = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResponseAsync("hello"));

        Assert.StartsWith("User-Agent header: OpenAI", e.Message);
        Assert.Contains("MEAI", e.Message);
    }

    private static IChatClient CreateResponseClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(
            new ApiKeyCredential("apikey"),
            new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetOpenAIResponseClient(modelId)
        .AsIChatClient();

    private static string ResponseStatusToRequestValue(ResponseStatus status)
    {
        if (status == ResponseStatus.InProgress)
        {
            return "in_progress";
        }

        return status.ToString().ToLowerInvariant();
    }

    private sealed class TestOpenAIResponsesContinuationToken : ResponseContinuationToken
    {
        internal TestOpenAIResponsesContinuationToken(string responseId)
        {
            ResponseId = responseId;
        }

        /// <summary>Gets or sets the Id of the response.</summary>
        internal string ResponseId { get; set; }

        /// <summary>Gets or sets the sequence number of a streamed update.</summary>
        internal int? SequenceNumber { get; set; }

        internal static TestOpenAIResponsesContinuationToken FromToken(object token)
        {
            if (token is TestOpenAIResponsesContinuationToken testOpenAIResponsesContinuationToken)
            {
                return testOpenAIResponsesContinuationToken;
            }

            if (token is not ResponseContinuationToken)
            {
                throw new ArgumentException("Failed to create OpenAIResponsesResumptionToken from provided token because it is not of type ResponseContinuationToken.", nameof(token));
            }

            ReadOnlyMemory<byte> data = ((ResponseContinuationToken)token).ToBytes();

            Utf8JsonReader reader = new(data.Span);

            string responseId = null!;
            int? startAfter = null;

            _ = reader.Read();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                string propertyName = reader.GetString()!;

                switch (propertyName)
                {
                    case "responseId":
                        _ = reader.Read();
                        responseId = reader.GetString()!;
                        break;
                    case "sequenceNumber":
                        _ = reader.Read();
                        startAfter = reader.GetInt32();
                        break;
                    default:
                        throw new JsonException($"Unrecognized property '{propertyName}'.");
                }
            }

            return new(responseId)
            {
                SequenceNumber = startAfter
            };
        }

        public override ReadOnlyMemory<byte> ToBytes()
        {
            using MemoryStream stream = new();
            using Utf8JsonWriter writer = new(stream);

            writer.WriteStartObject();

            writer.WriteString("responseId", ResponseId);

            if (SequenceNumber.HasValue)
            {
                writer.WriteNumber("sequenceNumber", SequenceNumber.Value);
            }

            writer.WriteEndObject();

            writer.Flush();
            stream.Position = 0;

            return stream.ToArray();
        }
    }
}
