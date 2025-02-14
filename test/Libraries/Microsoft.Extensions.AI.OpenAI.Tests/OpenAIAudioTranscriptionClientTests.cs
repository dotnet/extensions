// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI;
using OpenAI.Audio;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAIAudioTranscriptionClientTests
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => new OpenAIAudioTranscriptionClient(null!, "model"));
        Assert.Throws<ArgumentNullException>("audioClient", () => new OpenAIAudioTranscriptionClient(null!));

        OpenAIClient openAIClient = new("key");
        Assert.Throws<ArgumentNullException>("modelId", () => new OpenAIAudioTranscriptionClient(openAIClient, null!));
        Assert.Throws<ArgumentException>("modelId", () => new OpenAIAudioTranscriptionClient(openAIClient, ""));
        Assert.Throws<ArgumentException>("modelId", () => new OpenAIAudioTranscriptionClient(openAIClient, "   "));
    }

    [Fact]
    public void AsAudioTranscriptionClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsAudioTranscriptionClient("model"));
        Assert.Throws<ArgumentNullException>("audioClient", () => ((AudioClient)null!).AsAudioTranscriptionClient());

        OpenAIClient client = new("key");
        Assert.Throws<ArgumentNullException>("modelId", () => client.AsAudioTranscriptionClient(null!));
        Assert.Throws<ArgumentException>("modelId", () => client.AsAudioTranscriptionClient("   "));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsAudioTranscriptionClient_OpenAIClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var openAIClient = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IAudioTranscriptionClient client = openAIClient.AsAudioTranscriptionClient(model);
        var metadata = client.GetService<ChatClientMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
        Assert.Equal(model, metadata.ModelId);

        client = openAIClient.GetAudioClient(model).AsAudioTranscriptionClient();
        metadata = client.GetService<ChatClientMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
        Assert.Equal(model, metadata.ModelId);
    }

    [Fact]
    public void GetService_OpenAIClient_SuccessfullyReturnsUnderlyingClient()
    {
        OpenAIClient openAIClient = new(new ApiKeyCredential("key"));
        IAudioTranscriptionClient client = openAIClient.AsAudioTranscriptionClient("model");

        Assert.Same(client, client.GetService<IAudioTranscriptionClient>());
        Assert.Same(client, client.GetService<OpenAIAudioTranscriptionClient>());

        Assert.Same(openAIClient, client.GetService<OpenAIClient>());

        Assert.NotNull(client.GetService<AudioClient>());
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        using IAudioTranscriptionClient pipeline = client
            .AsBuilder()
            .UseLogging(mockLoggerFactory.Object)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingAudioTranscriptionClient>());

        Assert.Same(openAIClient, pipeline.GetService<OpenAIClient>());
        Assert.IsType<LoggingAudioTranscriptionClient>(pipeline.GetService<IAudioTranscriptionClient>());
    }

    [Fact]
    public void GetService_AudioClient_SuccessfullyReturnsUnderlyingClient()
    {
        AudioClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetAudioClient("model");
        IAudioTranscriptionClient audioClient = openAIClient.AsAudioTranscriptionClient();

        Assert.Same(audioClient, audioClient.GetService<IAudioTranscriptionClient>());
        Assert.Same(openAIClient, audioClient.GetService<AudioClient>());

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        using IAudioTranscriptionClient pipeline = audioClient
            .AsBuilder()
            .UseLogging(mockLoggerFactory.Object)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingAudioTranscriptionClient>());

        Assert.Same(openAIClient, pipeline.GetService<AudioClient>());
        Assert.IsType<LoggingAudioTranscriptionClient>(pipeline.GetService<IAudioTranscriptionClient>());
    }

    [Fact]
    public async Task BasicRequestResponse_NonStreaming()
    {
        static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(request.Content);

            var requestBody = await request.Content
#if NET
            .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            .ReadAsStringAsync().ConfigureAwait(false);
#endif
            Assert.Contains("filename=file.wav", requestBody);

            const string Output = """
            {"text":"I finally got back to the gym the other day."}
            """;

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content: Output, encoding: Encoding.UTF8, mediaType: "application/json"),
            };
        }

        using DelegatedHttpHandler handler = new(SendAsync);
        using HttpClient httpClient = new(handler);
        using IAudioTranscriptionClient client = CreateAudioTranscriptionClient(httpClient, "gpt-4o-mini");

        using var fileStream = GetAudioStream("audio001.wav");
        var response = await client.TranscribeAsync(fileStream, new()
        {
            AudioLanguage = "en-US",
        });
        Assert.NotNull(response);

        Assert.Single(response.Choices);
        Assert.Contains("I finally got back to the gym the other day", response.AudioTranscription.Text);

        Assert.NotNull(response.RawRepresentation);
        Assert.IsType<OpenAI.Audio.AudioTranscription>(response.RawRepresentation);
    }

    [Fact]
    public async Task BasicRequestResponse_Streaming()
    {
        // There's no support for streaming audio in the OpenAI API,
        // so we're just testing the client's ability to handle streaming responses.

        static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(request.Content);

            var requestBody = await request.Content
#if NET
            .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            .ReadAsStringAsync().ConfigureAwait(false);
#endif
            Assert.Contains("filename=file.wav", requestBody);

            const string Output = """
            {"text":"I finally got back to the gym the other day."}
            """;

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content: Output, encoding: Encoding.UTF8, mediaType: "application/json"),
            };
        }

        using DelegatedHttpHandler handler = new(SendAsync);
        using HttpClient httpClient = new(handler);
        using IAudioTranscriptionClient client = CreateAudioTranscriptionClient(httpClient, "gpt-4o-mini");

        using var fileStream = GetAudioStream("audio001.wav");
        await foreach (var update in client.TranscribeStreamingAsync(fileStream, new() { AudioLanguage = "en-US" }))
        {
            Assert.Contains("I finally got back to the gym the other day", update.Text);
            Assert.NotNull(update.RawRepresentation);
            Assert.IsType<OpenAI.Audio.AudioTranscription>(update.RawRepresentation);
            Assert.Equal(0, update.InputIndex);
        }
    }

#pragma warning disable S125 // Sections of code should not be commented out
    /*
        [Fact]
        public async Task NonStronglyTypedOptions_AllSent()
        {
            const string Input = """
                {"messages":[{"role":"user","content":"hello"}],
                "model":"gpt-4o-mini",
                "store":true,
                "metadata":{"something":"else"},
                "logit_bias":{"12":34},
                "logprobs":true,
                "top_logprobs":42,
                "parallel_tool_calls":false,
                "user":"12345"}
                """;

            const string Output = """
                {
                  "id": "chatcmpl-ADx3PvAnCwJg0woha4pYsBTi3ZpOI",
                  "object": "chat.completion",
                  "model": "gpt-4o-mini-2024-07-18",
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": "Hello! How can I assist you today?"
                      },
                      "finish_reason": "stop"
                    }
                  ]
                }
                """;

            using VerbatimHttpHandler handler = new(Input, Output);
            using HttpClient httpClient = new(handler);
            using IAudioTranscriptionClient client = CreateAudioTranscriptionClient(httpClient, "gpt-4o-mini");

            Assert.NotNull(await client.CompleteAsync("hello", new()
            {
                AdditionalProperties = new()
                {
                    ["StoredOutputEnabled"] = true,
                    ["Metadata"] = new Dictionary<string, string>
                    {
                        ["something"] = "else",
                    },
                    ["LogitBiases"] = new Dictionary<int, int> { { 12, 34 } },
                    ["IncludeLogProbabilities"] = true,
                    ["TopLogProbabilityCount"] = 42,
                    ["AllowParallelToolCalls"] = false,
                    ["EndUserId"] = "12345",
                },
            }));
        }

        [Fact]
        public async Task MultipleMessages_NonStreaming()
        {
            const string Input = """
                {
                    "messages": [
                        {
                            "role": "system",
                            "content": "You are a really nice friend."
                        },
                        {
                            "role": "user",
                            "content": "hello!"
                        },
                        {
                            "role": "assistant",
                            "content": "hi, how are you?"
                        },
                        {
                            "role": "user",
                            "content": "i\u0027m good. how are you?"
                        }
                    ],
                    "model": "gpt-4o-mini",
                    "frequency_penalty": 0.75,
                    "presence_penalty": 0.5,
                    "seed":42,
                    "stop": [
                        "great"
                    ],
                    "temperature": 0.25
                }
                """;

            const string Output = """
                {
                  "id": "chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P",
                  "object": "chat.completion",
                  "created": 1727894187,
                  "model": "gpt-4o-mini-2024-07-18",
                  "choices": [
                    {
                      "index": 0,
                      "message": {
                        "role": "assistant",
                        "content": "I’m doing well, thank you! What’s on your mind today?",
                        "refusal": null
                      },
                      "logprobs": null,
                      "finish_reason": "stop"
                    }
                  ],
                  "usage": {
                    "prompt_tokens": 42,
                    "completion_tokens": 15,
                    "total_tokens": 57,
                    "prompt_tokens_details": {
                      "cached_tokens": 13,
                      "audio_tokens": 123
                    },
                    "completion_tokens_details": {
                      "reasoning_tokens": 90,
                      "audio_tokens": 456
                    }
                  },
                  "system_fingerprint": "fp_f85bea6784"
                }
                """;

            using VerbatimHttpHandler handler = new(Input, Output);
            using HttpClient httpClient = new(handler);
            using IAudioTranscriptionClient client = CreateAudioTranscriptionClient(httpClient, "gpt-4o-mini");

            List<ChatMessage> messages =
            [
                new(ChatRole.System, "You are a really nice friend."),
                new(ChatRole.User, "hello!"),
                new(ChatRole.Assistant, "hi, how are you?"),
                new(ChatRole.User, "i'm good. how are you?"),
            ];

            var response = await client.CompleteAsync(messages, new()
            {
                Temperature = 0.25f,
                FrequencyPenalty = 0.75f,
                PresencePenalty = 0.5f,
                StopSequences = ["great"],
                Seed = 42,
            });
            Assert.NotNull(response);

            Assert.Equal("chatcmpl-ADyV17bXeSm5rzUx3n46O7m3M0o3P", response.CompletionId);
            Assert.Equal("I’m doing well, thank you! What’s on your mind today?", response.Message.Text);
            Assert.Single(response.Message.Contents);
            Assert.Equal(ChatRole.Assistant, response.Message.Role);
            Assert.Equal("gpt-4o-mini-2024-07-18", response.ModelId);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_727_894_187), response.CreatedAt);
            Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

            Assert.NotNull(response.Usage);
            Assert.Equal(42, response.Usage.InputTokenCount);
            Assert.Equal(15, response.Usage.OutputTokenCount);
            Assert.Equal(57, response.Usage.TotalTokenCount);
            Assert.Equal(new Dictionary<string, long>
            {
                { "InputTokenDetails.AudioTokenCount", 123 },
                { "InputTokenDetails.CachedTokenCount", 13 },
                { "OutputTokenDetails.AudioTokenCount", 456 },
                { "OutputTokenDetails.ReasoningTokenCount", 90 },
            }, response.Usage.AdditionalCounts);

            Assert.NotNull(response.AdditionalProperties);
            Assert.Equal("fp_f85bea6784", response.AdditionalProperties[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)]);
        }
        */

    private static Stream GetAudioStream(string fileName)
#pragma warning restore S125 // Sections of code should not be commented out
    {
        using Stream? s = typeof(AudioTranscriptionClientIntegrationTests).Assembly.GetManifestResourceStream($"Microsoft.Extensions.AI.Resources.{fileName}");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);

        ms.Position = 0;
        return ms;
    }

    private static IAudioTranscriptionClient CreateAudioTranscriptionClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .AsAudioTranscriptionClient(modelId);
}
