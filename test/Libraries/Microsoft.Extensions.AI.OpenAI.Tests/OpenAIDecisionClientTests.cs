// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Decisions;
using Xunit;

#pragma warning disable OPENAI001 // The OpenAI Decisions API is experimental
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAIDecisionClientTests
{
    private const string RedPixelPng = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAAKElEQVR4nO3NsQ0AAAzCMP5/un0CNkuZ41wybXsHAAAAAAAAAAAAxR4yw/wuPL6QkAAAAABJRU5ErkJggg==";

    [Fact]
    public void AsIDecisionClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("decisionsClient", () => ((DecisionsClient)null!).AsIDecisionClient());
    }

    [Fact]
    public void AsIDecisionClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        DecisionsClient decisionsClient = new(new ApiKeyCredential("key"), new DecisionsClientOptions { Endpoint = endpoint });

        using IDecisionClient client = decisionsClient.AsIDecisionClient("gpt-6-luna");
        var metadata = client.GetService<DecisionClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
        Assert.Equal("gpt-6-luna", metadata.DefaultModelId);
        Assert.Equal([DecisionKind.Binary, DecisionKind.Choice, DecisionKind.Score], metadata.SupportedKinds);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        DecisionsClient decisionsClient = new(new ApiKeyCredential("key"));
        using IDecisionClient client = decisionsClient.AsIDecisionClient("gpt-6-luna");

        Assert.Same(client, client.GetService<IDecisionClient>());
        Assert.Same(decisionsClient, client.GetService<DecisionsClient>());
        Assert.NotNull(client.GetService<DecisionClientMetadata>());
        Assert.Null(client.GetService<DecisionsClient>("key"));
        Assert.Null(client.GetService<string>());
        Assert.Throws<ArgumentNullException>("serviceType", () => client.GetService(null!));
    }

    [Fact]
    public async Task GetDecisionAsync_MapsRequestAndResponse()
    {
        const string Input = $$"""
            {
              "input": [
                {
                  "type": "message",
                  "role": "user",
                  "content": [
                    { "type": "input_json", "data": [ { "ticket": "My package never arrived." } ] },
                    { "type": "input_text", "text": "Evaluate the ticket and the photo." },
                    { "type": "input_image", "image_url": "data:image/png;base64,{{RedPixelPng}}" },
                    { "type": "input_image", "image_url": "https://example.com/photo.png", "detail": "low" }
                  ]
                }
              ],
              "model": "gpt-6-luna",
              "questions": [
                {
                  "type": "predicate",
                  "instructions": [
                    { "type": "input_text", "text": "The photo shows a damaged package." },
                    { "type": "input_text", "text": "If true: " },
                    { "type": "input_text", "text": "Damage is visible." },
                    { "type": "input_text", "text": "If false: " },
                    { "type": "input_text", "text": "No damage is visible." }
                  ],
                  "name": "damaged"
                },
                {
                  "type": "choice",
                  "choices": [
                    { "value": "billing", "description": "Payments and invoices." },
                    { "value": "shipping", "description": "Delivery and tracking." },
                    { "value": "other", "description": "Anything else." }
                  ],
                  "instructions": [
                    { "type": "input_text", "text": "Which category best describes the ticket?" },
                    { "type": "input_image", "image_url": "https://example.com/reference.png", "detail": "high" }
                  ],
                  "name": "category"
                },
                {
                  "type": "score",
                  "instructions": [ { "type": "input_text", "text": "How urgent is the ticket?" } ],
                  "levels": [
                    { "label": "low", "description": "Can wait." },
                    { "label": "medium", "description": "Soon." },
                    { "label": "high", "description": "Immediately." }
                  ],
                  "name": "urgency"
                }
              ]
            }
            """;

        const string Output = """
            {
              "model": "gpt-6-luna",
              "answers": [
                { "type": "predicate", "name": "damaged", "probability": 0.25 },
                {
                  "type": "choice",
                  "name": "category",
                  "choice": "shipping",
                  "probabilities": [
                    { "value": "shipping", "probability": 0.9 },
                    { "value": "billing", "probability": 0.1 }
                  ],
                  "confidence": 0.8
                },
                {
                  "type": "score",
                  "name": "urgency",
                  "score": 1.9,
                  "probabilities": [
                    { "value": 0, "label": "low", "probability": 0.0 },
                    { "value": 1, "label": "medium", "probability": 0.1 },
                    { "value": 2, "label": "high", "probability": 0.9 }
                  ],
                  "confidence": 0.95
                }
              ],
              "usage": {
                "input_tokens": 428,
                "input_tokens_details": { "cached_tokens": 12, "cache_write_tokens": 0 },
                "output_tokens": 3,
                "output_tokens_details": { "reasoning_tokens": 2 },
                "total_tokens": 431
              }
            }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IDecisionClient client = CreateDecisionClient(httpClient, "gpt-6-luna");

        DecisionRequest request = new(
            JsonDocument.Parse("""{ "ticket": "My package never arrived." }""").RootElement,
            [
                new BinaryDecisionQuestion("damaged", "The photo shows a damaged package.", "Damage is visible.", "No damage is visible."),
                new ChoiceDecisionQuestion(
                    "category",
                    [
                        new TextContent("Which category best describes the ticket?"),
                        new UriContent("https://example.com/reference.png", "image/png") { AdditionalProperties = new() { ["detail"] = "high" } },
                    ],
                    [
                        new DecisionCandidate("billing", "Payments and invoices."),
                        new DecisionCandidate("shipping", "Delivery and tracking."),
                        new DecisionCandidate("other", "Anything else."),
                    ]),
                new ScoreDecisionQuestion("urgency", "How urgent is the ticket?",
                [
                    new DecisionScoreLevel("low", "Can wait."),
                    new DecisionScoreLevel("medium", "Soon."),
                    new DecisionScoreLevel("high", "Immediately."),
                ]),
            ],
            [
                new TextContent("Evaluate the ticket and the photo."),
                new DataContent(Convert.FromBase64String(RedPixelPng), "image/png"),
                new UriContent("https://example.com/photo.png", "image/png") { AdditionalProperties = new() { ["detail"] = "low" } },
            ]);

        DecisionResponse response = await client.GetDecisionAsync(request);

        Assert.Equal("gpt-6-luna", response.ModelId);
        Assert.IsType<DecisionResource>(response.RawRepresentation);
        Assert.Equal(3, response.Answers.Count);

        var binary = Assert.IsType<BinaryDecisionAnswer>(response.Answers[0]);
        Assert.Equal("damaged", binary.QuestionId);
        Assert.Equal(0.25, binary.TrueProbability);
        Assert.IsType<DecisionsPredicateAnswer>(binary.RawRepresentation);

        var choice = Assert.IsType<ChoiceDecisionAnswer>(response.Answers[1]);
        Assert.Equal("shipping", choice.SelectedCandidateId);
        Assert.Equal(["billing", "shipping", "other"], choice.Probabilities.Select(p => p.Id));
        Assert.Equal([0.1, 0.9, 0d], choice.Probabilities.Select(p => p.Value));
        Assert.Equal(0.8, choice.AdditionalProperties?["confidence"]);

        var score = Assert.IsType<ScoreDecisionAnswer>(response.Answers[2]);
        Assert.Equal(1.9, score.Score);
        Assert.Equal(["low", "medium", "high"], score.Probabilities.Select(p => p.Id));
        Assert.Equal([0d, 0.1, 0.9], score.Probabilities.Select(p => p.Value));
        Assert.Equal(0.95, score.AdditionalProperties?["confidence"]);

        Assert.NotNull(response.Usage);
        Assert.Equal(428, response.Usage.InputTokenCount);
        Assert.Equal(3, response.Usage.OutputTokenCount);
        Assert.Equal(431, response.Usage.TotalTokenCount);
        Assert.Equal(12, response.Usage.CachedInputTokenCount);
        Assert.Equal(2, response.Usage.ReasoningTokenCount);
    }

    [Fact]
    public async Task GetDecisionAsync_RefusalMapsToUnansweredAnswer()
    {
        const string Output = """
            {
              "model": "gpt-6-luna",
              "answers": [ { "type": "refusal", "name": "safe" } ]
            }
            """;

        using IDecisionClient client = CreateDecisionClient(Output);

        DecisionResponse response = await client.GetDecisionAsync(Request([new BinaryDecisionQuestion("safe", "The content is safe.")]));

        var answer = Assert.IsType<UnansweredDecisionAnswer>(Assert.Single(response.Answers));
        Assert.Equal("safe", answer.QuestionId);
        Assert.Equal(DecisionKind.Binary, answer.Kind);
        Assert.Equal(DecisionUnansweredReason.Refusal, answer.Reason);
        Assert.IsType<DecisionsRefusalAnswer>(answer.RawRepresentation);
    }

    [Fact]
    public async Task GetDecisionAsync_UsesModelFromOptionsAndRawRepresentationFactory()
    {
        const string Input = """
            {
              "input": [ { "type": "message", "role": "user", "content": [ { "type": "input_text", "text": "hello" } ] } ],
              "model": "from-options",
              "prompt_cache_key": "cache-key",
              "questions": [ { "type": "predicate", "instructions": [ { "type": "input_text", "text": "Is it a greeting?" } ], "name": "greeting" } ]
            }
            """;

        const string Output = """
            { "model": "from-options", "answers": [ { "type": "predicate", "name": "greeting", "probability": 1.0 } ] }
            """;

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using IDecisionClient client = CreateDecisionClient(httpClient, defaultModelId: null);

        DecisionResponse response = await client.GetDecisionAsync(
            Request([new BinaryDecisionQuestion("greeting", "Is it a greeting?")], [new TextContent("hello")]),
            new DecisionOptions
            {
                ModelId = "from-options",
                RawRepresentationFactory = _ => new DecisionCreationOptions { PromptCacheKey = "cache-key" },
            });

        Assert.Equal(1d, Assert.IsType<BinaryDecisionAnswer>(Assert.Single(response.Answers)).TrueProbability);
    }

    [Theory]
    [InlineData("""{ "status": "delivered" }""", """{ "type": "input_json", "data": [ { "status": "delivered" } ] },""")]
    [InlineData("\"The order arrived late.\"", """{ "type": "input_text", "text": "The order arrived late." },""")]
    [InlineData("[1,2]", """{ "type": "input_text", "text": "[1,2]" },""")]
    [InlineData("42", """{ "type": "input_text", "text": "42" },""")]
    [InlineData("null", "")]
    public async Task GetDecisionAsync_MapsStateByJsonKind(string stateJson, string expectedStatePart)
    {
        string input = $$"""
            {
              "input": [ { "type": "message", "role": "user", "content": [ {{expectedStatePart}} { "type": "input_text", "text": "input" } ] } ],
              "model": "gpt-6-luna",
              "questions": [ { "type": "predicate", "instructions": [ { "type": "input_text", "text": "Is it?" } ], "name": "q" } ]
            }
            """;

        const string Output = """
            { "model": "gpt-6-luna", "answers": [ { "type": "predicate", "name": "q", "probability": 1.0 } ] }
            """;

        using VerbatimHttpHandler handler = new(input, Output);
        using HttpClient httpClient = new(handler);
        using IDecisionClient client = CreateDecisionClient(httpClient, "gpt-6-luna");
        using JsonDocument state = JsonDocument.Parse(stateJson);

        DecisionResponse response = await client.GetDecisionAsync(
            new DecisionRequest(state.RootElement, [new BinaryDecisionQuestion("q", "Is it?")], [new TextContent("input")]));

        Assert.Equal(1d, Assert.IsType<BinaryDecisionAnswer>(Assert.Single(response.Answers)).TrueProbability);
    }

    [Fact]
    public async Task GetDecisionAsync_NoModel_Throws()
    {
        using IDecisionClient client = CreateDecisionClient("{}", defaultModelId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetDecisionAsync(Request([new BinaryDecisionQuestion("q", "Is it?")])));
    }

    [Fact]
    public async Task GetDecisionAsync_UnsupportedContent_Throws()
    {
        using IDecisionClient client = CreateDecisionClient("{}");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.GetDecisionAsync(Request([new BinaryDecisionQuestion("q", "Is it?")], [new DataContent(new byte[] { 1, 2, 3 }, "audio/wav")])));

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.GetDecisionAsync(Request([new ChoiceDecisionQuestion("q", "Which?",
                [new DecisionCandidate("a", [new UriContent("https://example.com/a.png", "image/png")]), new DecisionCandidate("b", "B")])])));
    }

    [Theory]
    [InlineData("""{ "answers": [] }""")]
    [InlineData("""{ "answers": [ { "type": "predicate", "name": "other", "probability": 0.5 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "predicate", "name": "q", "probability": 0.5 }, { "type": "predicate", "name": "other", "probability": 0.5 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "predicate", "probability": 0.5 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "predicate", "name": "q", "probability": 0.5 }, { "type": "predicate", "name": "q", "probability": 0.5 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "choice", "name": "q", "choice": "a", "probabilities": [ { "value": "a", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    public async Task GetDecisionAsync_InvalidBinaryAnswers_ThrowsProtocolException(string output)
    {
        using IDecisionClient client = CreateDecisionClient(output);

        await Assert.ThrowsAsync<DecisionProtocolException>(() =>
            client.GetDecisionAsync(Request([new BinaryDecisionQuestion("q", "Is it?")])));
    }

    [Theory]
    [InlineData("""{ "answers": [ { "type": "choice", "name": "q", "choice": "c", "probabilities": [ { "value": "a", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "choice", "name": "q", "choice": "a", "probabilities": [ { "value": "c", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "choice", "name": "q", "choice": "a", "probabilities": [ { "value": "a", "probability": 0.5 }, { "value": "a", "probability": 0.5 } ], "confidence": 1.0 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "choice", "name": "q", "choice": true, "probabilities": [ { "value": "a", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    public async Task GetDecisionAsync_InvalidChoiceAnswers_ThrowsProtocolException(string output)
    {
        using IDecisionClient client = CreateDecisionClient(output);

        await Assert.ThrowsAsync<DecisionProtocolException>(() =>
            client.GetDecisionAsync(Request([new ChoiceDecisionQuestion("q", "Which?", [new DecisionCandidate("a", "A"), new DecisionCandidate("b", "B")])])));
    }

    [Theory]
    [InlineData("""{ "answers": [ { "type": "score", "name": "q", "score": 1.0, "probabilities": [ { "value": 1, "label": "low", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "score", "name": "q", "score": 1.0, "probabilities": [ { "value": 2, "label": "extreme", "probability": 1.0 } ], "confidence": 1.0 } ] }""")]
    [InlineData("""{ "answers": [ { "type": "score", "name": "q", "score": 0.0, "probabilities": [ { "value": 0, "label": "low", "probability": 0.0 }, { "value": 1, "label": "high", "probability": 0.0 } ], "confidence": 1.0 } ] }""")]
    public async Task GetDecisionAsync_InvalidScoreAnswers_ThrowsProtocolException(string output)
    {
        using IDecisionClient client = CreateDecisionClient(output);

        await Assert.ThrowsAsync<DecisionProtocolException>(() =>
            client.GetDecisionAsync(Request([new ScoreDecisionQuestion("q", "How much?", [new DecisionScoreLevel("low", "Low"), new DecisionScoreLevel("high", "High")])])));
    }

    [Fact]
    public async Task GetDecisionAsync_ScoreIsClampedToLevelScale()
    {
        const string Output = """
            { "answers": [ { "type": "score", "name": "q", "score": 1.0000001, "probabilities": [ { "value": 0, "label": "low", "probability": 0.0 }, { "value": 1, "label": "high", "probability": 1.0 } ], "confidence": 1.0 } ] }
            """;

        using IDecisionClient client = CreateDecisionClient(Output);

        DecisionResponse response = await client.GetDecisionAsync(
            Request([new ScoreDecisionQuestion("q", "How much?", [new DecisionScoreLevel("low", "Low"), new DecisionScoreLevel("high", "High")])]));

        Assert.Equal(1d, Assert.IsType<ScoreDecisionAnswer>(Assert.Single(response.Answers)).Score);
    }

    [Theory]
    [InlineData((HttpStatusCode)429, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public async Task GetDecisionAsync_ServiceError_ThrowsDecisionClientException(HttpStatusCode statusCode, bool isTransient)
    {
        using IDecisionClient client = CreateDecisionClient("""{ "error": { "message": "failure" } }""", statusCode: statusCode);

        var ex = await Assert.ThrowsAsync<DecisionClientException>(() =>
            client.GetDecisionAsync(Request([new BinaryDecisionQuestion("q", "Is it?")])));

        Assert.Equal(isTransient, ex.IsTransient);
        Assert.IsType<ClientResultException>(ex.InnerException);
    }

    private static DecisionRequest Request(IReadOnlyList<DecisionQuestion> questions, IReadOnlyList<AIContent>? contents = null) =>
        new(null, questions, contents ?? [new TextContent("input")]);

    private static IDecisionClient CreateDecisionClient(HttpClient httpClient, string? defaultModelId) =>
        new DecisionsClient(
            new ApiKeyCredential("apikey"),
            new DecisionsClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient),
                RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
            })
        .AsIDecisionClient(defaultModelId);

    private static IDecisionClient CreateDecisionClient(string output, string? defaultModelId = "gpt-6-luna", HttpStatusCode statusCode = HttpStatusCode.OK) =>
        CreateDecisionClient(new HttpClient(new StaticResponseHandler(statusCode, output)), defaultModelId);

    private sealed class StaticResponseHandler(HttpStatusCode statusCode, string output) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(output, Encoding.UTF8, "application/json") });
    }
}
