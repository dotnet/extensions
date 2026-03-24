// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;
using Xunit;

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Microsoft.Extensions.AI;

public class OpenAITextToSpeechClientTests
{
    [Fact]
    public void AsITextToSpeechClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("audioClient", () => ((AudioClient)null!).AsITextToSpeechClient());
    }

    [Fact]
    public void AsITextToSpeechClient_AudioClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "tts-1";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        ITextToSpeechClient ttsClient = client.GetAudioClient(model).AsITextToSpeechClient();
        var metadata = ttsClient.GetService<TextToSpeechClientMetadata>();
        Assert.Equal("openai", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_AudioClient_SuccessfullyReturnsUnderlyingClient()
    {
        AudioClient audioClient = new OpenAIClient(new ApiKeyCredential("key")).GetAudioClient("tts-1");
        ITextToSpeechClient ttsClient = audioClient.AsITextToSpeechClient();
        Assert.Same(ttsClient, ttsClient.GetService<ITextToSpeechClient>());
        Assert.Same(audioClient, ttsClient.GetService<AudioClient>());
        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        using ITextToSpeechClient pipeline = ttsClient
            .AsBuilder()
            .UseLogging(factory)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingTextToSpeechClient>());

        Assert.Same(audioClient, pipeline.GetService<AudioClient>());
        Assert.IsType<LoggingTextToSpeechClient>(pipeline.GetService<ITextToSpeechClient>());
    }

    [Fact]
    public async Task GetAudioAsync_DefaultVoice_BasicRequestResponse()
    {
        const string Input = """
                {
                    "model": "tts-1",
                    "input": "Hello world",
                    "voice": "alloy"
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        var response = await client.GetAudioAsync("Hello world");

        Assert.NotNull(response);
        Assert.Equal("tts-1", response.ModelId);
        Assert.NotNull(response.RawRepresentation);
        Assert.Single(response.Contents);
        var content = Assert.IsType<DataContent>(response.Contents[0]);
        Assert.Equal("audio/mpeg", content.MediaType);
        Assert.True(content.Data.Length > 0);
    }

    [Fact]
    public async Task GetAudioAsync_CustomVoice_SetsVoice()
    {
        const string Input = """
                {
                    "model": "tts-1",
                    "input": "Hello world",
                    "voice": "nova"
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        var response = await client.GetAudioAsync("Hello world", new TextToSpeechOptions
        {
            VoiceId = "nova"
        });

        Assert.NotNull(response);
        Assert.Single(response.Contents);
        var content = Assert.IsType<DataContent>(response.Contents[0]);
        Assert.Equal("audio/mpeg", content.MediaType);
    }

    [Fact]
    public async Task GetAudioAsync_SpeedMapping_SetsSpeedRatio()
    {
        const string Input = """
                {
                    "model": "tts-1",
                    "input": "Hello world",
                    "voice": "alloy",
                    "speed": 1.5
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        var response = await client.GetAudioAsync("Hello world", new TextToSpeechOptions
        {
            Speed = 1.5f
        });

        Assert.NotNull(response);
        Assert.Single(response.Contents);
    }

    [Theory]
    [InlineData("opus", "audio/opus")]
    [InlineData("wav", "audio/wav")]
    [InlineData("mp3", "audio/mpeg")]
    [InlineData("aac", "audio/aac")]
    [InlineData("flac", "audio/flac")]
    [InlineData("pcm", "audio/l16")]
    public async Task GetAudioAsync_AudioFormat_SetsFormatAndMediaType(string audioFormat, string expectedMediaType)
    {
        string input = $$"""
                {
                    "model": "tts-1",
                    "input": "Hello world",
                    "voice": "alloy",
                    "response_format": "{{audioFormat}}"
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        var response = await client.GetAudioAsync("Hello world", new TextToSpeechOptions
        {
            AudioFormat = audioFormat
        });

        Assert.NotNull(response);
        Assert.Single(response.Contents);
        var content = Assert.IsType<DataContent>(response.Contents[0]);
        Assert.Equal(expectedMediaType, content.MediaType);
    }

    [Fact]
    public async Task GetAudioAsync_StronglyTypedOptions_AllSent()
    {
        const string Input = """
                {
                    "model": "tts-1",
                    "input": "Hello world",
                    "voice": "echo",
                    "speed": 1.5,
                    "response_format": "opus"
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        var response = await client.GetAudioAsync("Hello world", new()
        {
            VoiceId = "echo",
            RawRepresentationFactory = (s) =>
            new SpeechGenerationOptions
            {
                SpeedRatio = 1.5f,
                ResponseFormat = GeneratedSpeechFormat.Opus
            }
        });

        Assert.NotNull(response);
        Assert.Single(response.Contents);
        var content = Assert.IsType<DataContent>(response.Contents[0]);
        Assert.Equal("audio/opus", content.MediaType);
    }

    [Fact]
    public async Task GetAudioAsync_Cancelled_Throws()
    {
        using HttpClient httpClient = new();
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client.GetAudioAsync("Hello world", cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetStreamingAudioAsync_Cancelled_Throws()
    {
        using HttpClient httpClient = new();
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client
                .GetStreamingAudioAsync("Hello world", cancellationToken: cancellationTokenSource.Token)
                .GetAsyncEnumerator()
                .MoveNextAsync()
                .AsTask());
    }

    [Fact]
    public async Task GetStreamingAudioAsync_FallsBackToNonStreaming()
    {
        const string Input = """
                {
                    "model": "tts-1",
                    "input": "Hello streaming",
                    "voice": "alloy"
                }
                """;

        const string Output = "fake-audio-bytes";

        using VerbatimHttpHandler handler = new(Input, Output);
        using HttpClient httpClient = new(handler);
        using ITextToSpeechClient client = CreateTextToSpeechClient(httpClient, "tts-1");

        int updateCount = 0;
        await foreach (var update in client.GetStreamingAudioAsync("Hello streaming"))
        {
            updateCount++;
            Assert.NotNull(update);
            Assert.NotNull(update.RawRepresentation);
            Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdated, update.Kind);
            var content = update.Contents.OfType<DataContent>().Single();
            Assert.Equal("audio/mpeg", content.MediaType);
            Assert.True(content.Data.Length > 0);
        }

        Assert.Equal(1, updateCount);
    }

    private static ITextToSpeechClient CreateTextToSpeechClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
            .GetAudioClient(modelId)
            .AsITextToSpeechClient();
}
