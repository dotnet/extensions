// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;
using Xunit;

#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

public class OpenAISpeechToTextClientTests
{
    [Fact]
    public void AsISpeechToTextClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("audioClient", () => ((AudioClient)null!).AsISpeechToTextClient());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsISpeechToTextClient_AudioClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var client = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        ISpeechToTextClient speechToTextClient = client.GetAudioClient(model).AsISpeechToTextClient();
        var metadata = speechToTextClient.GetService<SpeechToTextClientMetadata>();
        Assert.Equal("openai", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_AudioClient_SuccessfullyReturnsUnderlyingClient()
    {
        AudioClient audioClient = new OpenAIClient(new ApiKeyCredential("key")).GetAudioClient("model");
        ISpeechToTextClient speechToTextClient = audioClient.AsISpeechToTextClient();
        Assert.Same(speechToTextClient, speechToTextClient.GetService<ISpeechToTextClient>());
        Assert.Same(audioClient, speechToTextClient.GetService<AudioClient>());
        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        using ISpeechToTextClient pipeline = speechToTextClient
            .AsBuilder()
            .UseLogging(factory)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingSpeechToTextClient>());

        Assert.Same(audioClient, pipeline.GetService<AudioClient>());
        Assert.IsType<LoggingSpeechToTextClient>(pipeline.GetService<ISpeechToTextClient>());
    }

    [Theory]
    [InlineData("pt", null)]
    [InlineData("en", null)]
    [InlineData("en", "en")]
    [InlineData("pt", "pt")]
    public async Task GetTextAsync_BasicRequestResponse(string? speechLanguage, string? textLanguage)
    {
        string input = $$"""
                {
                    "model": "whisper-1",
                    "language": "{{speechLanguage}}"
                }
                """;

        const string Output = """
                {
                    "text":"I finally got back to the gym the other day."
                }
                """;

        using VerbatimMultiPartHttpHandler handler = new(input, Output) { ExpectedRequestUriContains = "audio/transcriptions" };
        using HttpClient httpClient = new(handler);
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var audioSpeechStream = GetAudioStream();
        var response = await client.GetTextAsync(audioSpeechStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        });

        Assert.NotNull(response);

        Assert.Contains("I finally got back to the gym the other day", response.Text);

        Assert.NotNull(response.RawRepresentation);
        Assert.IsType<AudioTranscription>(response.RawRepresentation);
    }

    [Fact]
    public async Task GetTextAsync_Cancelled_Throws()
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client.GetTextAsync(fileStream, cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetStreamingTextAsync_Cancelled_Throws()
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client
                .GetStreamingTextAsync(fileStream, cancellationToken: cancellationTokenSource.Token)
                .GetAsyncEnumerator()
                .MoveNextAsync()
                .AsTask());
    }

    [Theory]
    [InlineData("pt", null)]
    [InlineData("en", null)]
    [InlineData("en", "en")]
    [InlineData("pt", "pt")]
    public async Task GetStreamingTextAsync_BasicRequestResponse(string? speechLanguage, string? textLanguage)
    {
        // There's no support for streaming audio in the OpenAI API,
        // so we're just testing the client's ability to handle streaming responses.

        string input = $$"""
                {
                    "model": "whisper-1",
                    "language": "{{speechLanguage}}"
                }
                """;

        const string Output = """
                {
                    "text":"I finally got back to the gym the other day."
                }
                """;

        using VerbatimMultiPartHttpHandler handler = new(input, Output) { ExpectedRequestUriContains = "audio/transcriptions" };
        using HttpClient httpClient = new(handler);
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var audioSpeechStream = GetAudioStream();
        await foreach (var update in client.GetStreamingTextAsync(audioSpeechStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        }))
        {
            Assert.Contains("I finally got back to the gym the other day", update.Text);
            Assert.NotNull(update.RawRepresentation);
            Assert.IsType<OpenAI.Audio.AudioTranscription>(update.RawRepresentation);
        }
    }

    [Fact]
    public async Task GetStreamingTextAsync_BasicTranslateRequestResponse()
    {
        string textLanguage = "en";

        // There's no support for non english translations, so no language is passed to the API.
        const string Input = $$"""
                {
                    "model": "whisper-1"
                }
                """;

        const string Output = """
                {
                    "text":"I finally got back to the gym the other day."
                }
                """;

        using VerbatimMultiPartHttpHandler handler = new(Input, Output) { ExpectedRequestUriContains = "audio/translations" };
        using HttpClient httpClient = new(handler);
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var audioSpeechStream = GetAudioStream();
        await foreach (var update in client.GetStreamingTextAsync(audioSpeechStream, new SpeechToTextOptions
        {
            SpeechLanguage = "pt",
            TextLanguage = textLanguage
        }))
        {
            Assert.Contains("I finally got back to the gym the other day", update.Text);
            Assert.NotNull(update.RawRepresentation);
            Assert.IsType<AudioTranslation>(update.RawRepresentation);
        }
    }

    [Fact]
    public async Task GetTextAsync_NonStronglyTypedOptions_AllSent()
    {
        const string Input = """
                {
                    "model": "whisper-1",
                    "prompt":"Hide any bad words with ",
                    "temperature": 0.5,
                    "response_format": "vtt",
                    "timestamp_granularities[]": ["word","segment"]
                }
                """;

        const string Output = """
                {
                    "text":"I finally got back to the gym the other day."
                }
                """;

        using VerbatimMultiPartHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var audioSpeechStream = GetAudioStream();
        Assert.NotNull(await client.GetTextAsync(audioSpeechStream, new()
        {
            AdditionalProperties = new()
            {
                ["Prompt"] = "Hide any bad words with ",
                ["SpeechLanguage"] = "pt",
                ["Temperature"] = 0.5f,
                ["TimestampGranularities"] = AudioTimestampGranularities.Segment | AudioTimestampGranularities.Word,
                ["ResponseFormat"] = AudioTranscriptionFormat.Vtt,
            },
        }));
    }

    [Fact]
    public async Task GetTextAsync_StronglyTypedOptions_AllSent()
    {
        const string Input = """
                {
                    "model": "whisper-1",
                    "language": "pt"
                }
                """;

        const string Output = """
                {
                    "text":"I finally got back to the gym the other day."
                }
                """;

        using VerbatimMultiPartHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var audioSpeechStream = GetAudioStream();
        Assert.NotNull(await client.GetTextAsync(audioSpeechStream, new()
        {
            SpeechLanguage = "pt",
        }));
    }

    private static Stream GetAudioStream()
        => new MemoryStream([0x01, 0x02]);

    private static ISpeechToTextClient CreateSpeechToTextClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
            .GetAudioClient(modelId)
            .AsISpeechToTextClient();
}
