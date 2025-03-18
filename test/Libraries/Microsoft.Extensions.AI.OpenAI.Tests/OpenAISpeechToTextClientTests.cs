﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

public class OpenAISpeechToTextClientTests
{
    [Fact]
    public void AsSpeechToTextClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("openAIClient", () => ((OpenAIClient)null!).AsSpeechToTextClient("model"));
        Assert.Throws<ArgumentNullException>("audioClient", () => ((AudioClient)null!).AsSpeechToTextClient());

        OpenAIClient client = new("key");
        Assert.Throws<ArgumentNullException>("modelId", () => client.AsSpeechToTextClient(null!));
        Assert.Throws<ArgumentException>("modelId", () => client.AsSpeechToTextClient("   "));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AsSpeechToTextClient_OpenAIClient_ProducesExpectedMetadata(bool useAzureOpenAI)
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "amazingModel";

        var openAIClient = useAzureOpenAI ?
            new AzureOpenAIClient(endpoint, new ApiKeyCredential("key")) :
            new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        ISpeechToTextClient client = openAIClient.AsSpeechToTextClient(model);
        var metadata = client.GetService<SpeechToTextClientMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
        Assert.Equal(model, metadata.ModelId);

        client = openAIClient.GetAudioClient(model).AsSpeechToTextClient();
        metadata = client.GetService<SpeechToTextClientMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
        Assert.Equal(model, metadata.ModelId);
    }

    [Fact]
    public void GetService_OpenAIClient_SuccessfullyReturnsUnderlyingClient()
    {
        OpenAIClient openAIClient = new(new ApiKeyCredential("key"));
        ISpeechToTextClient client = openAIClient.AsSpeechToTextClient("model");

        Assert.Same(client, client.GetService<ISpeechToTextClient>());

        Assert.Same(openAIClient, client.GetService<OpenAIClient>());

        Assert.NotNull(client.GetService<AudioClient>());
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        using ISpeechToTextClient pipeline = client
            .AsBuilder()
            .UseLogging(mockLoggerFactory.Object)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingSpeechToTextClient>());

        Assert.Same(openAIClient, pipeline.GetService<OpenAIClient>());
        Assert.IsType<LoggingSpeechToTextClient>(pipeline.GetService<ISpeechToTextClient>());
    }

    [Fact]
    public void GetService_AudioClient_SuccessfullyReturnsUnderlyingClient()
    {
        AudioClient openAIClient = new OpenAIClient(new ApiKeyCredential("key")).GetAudioClient("model");
        ISpeechToTextClient audioClient = openAIClient.AsSpeechToTextClient();

        Assert.Same(audioClient, audioClient.GetService<ISpeechToTextClient>());
        Assert.Same(openAIClient, audioClient.GetService<AudioClient>());

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        using ISpeechToTextClient pipeline = audioClient
            .AsBuilder()
            .UseLogging(mockLoggerFactory.Object)
            .Build();

        Assert.NotNull(pipeline.GetService<LoggingSpeechToTextClient>());

        Assert.Same(openAIClient, pipeline.GetService<AudioClient>());
        Assert.IsType<LoggingSpeechToTextClient>(pipeline.GetService<ISpeechToTextClient>());
    }

    [Theory]
    [InlineData("pt", null)]
    [InlineData("en", null)]
    [InlineData("en", "en")]
    [InlineData("pt", "pt")]
    public async Task BasicTranscribeRequestResponse_NonStreaming(string? speechLanguage, string? textLanguage)
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

        using var fileStream = GetAudioStream("audio001.wav");
        var response = await client.GetResponseAsync(fileStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        });

        Assert.NotNull(response);

        Assert.Single(response.Choices);
        Assert.Contains("I finally got back to the gym the other day", response.Message.Text);

        Assert.NotNull(response.Message.RawRepresentation);
        Assert.IsType<OpenAI.Audio.AudioTranscription>(response.Message.RawRepresentation);
    }

    [Fact]
    public async Task CancelledBasicTranscribeRequestResponse_NonStreaming_Throw()
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream("audio001.wav");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client.GetResponseAsync(fileStream, cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task CancelledBasicTranscribeRequestResponse_Streaming_Throw()
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream("audio001.wav");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(()
            => client
                .GetStreamingResponseAsync(fileStream, cancellationToken: cancellationTokenSource.Token)
                .GetAsyncEnumerator()
                .MoveNextAsync()
                .AsTask());
    }

    [Theory]
    [InlineData("pt", null)]
    [InlineData("en", null)]
    [InlineData("en", "en")]
    [InlineData("pt", "pt")]
    public async Task BasicTranscribeRequestResponse_Streaming(string? speechLanguage, string? textLanguage)
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

        using var fileStream = GetAudioStream("audio001.mp3");
        await foreach (var update in client.GetStreamingResponseAsync(fileStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        }))
        {
            Assert.Contains("I finally got back to the gym the other day", update.Text);
            Assert.NotNull(update.RawRepresentation);
            Assert.IsType<OpenAI.Audio.AudioTranscription>(update.RawRepresentation);
            Assert.Equal(0, update.InputIndex);
        }
    }

    [Theory]
    [InlineData(null, "pt")]
    [InlineData(null, "it")]
    [InlineData("en", "pt")]
    public async Task NonSupportedTranslation_Streaming_Throws(string? speechLanguage, string? textLanguage)
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream("audio001.mp3");
        var asyncEnumerator = client.GetStreamingResponseAsync(fileStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        }).GetAsyncEnumerator();

        await Assert.ThrowsAsync<NotSupportedException>(() => asyncEnumerator.MoveNextAsync().AsTask());
    }

    [Theory]
    [InlineData(null, "pt")]
    [InlineData(null, "it")]
    [InlineData("en", "pt")]
    public async Task NonSupportedTranslation_NonStreaming_Throws(string? speechLanguage, string? textLanguage)
    {
        using HttpClient httpClient = new();
        using ISpeechToTextClient client = CreateSpeechToTextClient(httpClient, "whisper-1");

        using var fileStream = GetAudioStream("audio001.mp3");

        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetResponseAsync(fileStream, new SpeechToTextOptions
        {
            SpeechLanguage = speechLanguage,
            TextLanguage = textLanguage
        }));
    }

    [Fact]
    public async Task BasicTranslateRequestResponse_Streaming()
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

        using var fileStream = GetAudioStream("audio001.mp3");
        await foreach (var update in client.GetStreamingResponseAsync(fileStream, new SpeechToTextOptions
        {
            SpeechLanguage = "pt",
            TextLanguage = textLanguage
        }))
        {
            Assert.Contains("I finally got back to the gym the other day", update.Text);
            Assert.NotNull(update.RawRepresentation);
            Assert.IsType<OpenAI.Audio.AudioTranslation>(update.RawRepresentation);
            Assert.Equal(0, update.InputIndex);
        }
    }

    [Fact]
    public async Task NonStronglyTypedOptions_AllSent()
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

        using var fileStream = GetAudioStream("audio001.mp3");
        Assert.NotNull(await client.GetResponseAsync(fileStream, new()
        {
            AdditionalProperties = new()
            {
                ["SpeechLanguage"] = "pt",
                ["Temperature"] = 0.5f,
                ["TimestampGranularities"] = AudioTimestampGranularities.Segment | AudioTimestampGranularities.Word,
                ["Prompt"] = "Hide any bad words with ***",
                ["ResponseFormat"] = AudioTranscriptionFormat.Vtt,
            },
        }));
    }

    [Fact]
    public async Task StronglyTypedOptions_AllSent()
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

        using var fileStream = GetAudioStream("audio001.mp3");
        Assert.NotNull(await client.GetResponseAsync(fileStream, new()
        {
            SpeechLanguage = "pt",
        }));
    }

    private static Stream GetAudioStream(string fileName)
#pragma warning restore S125 // Sections of code should not be commented out
    {
        using Stream? s = typeof(OpenAISpeechToTextClientTests).Assembly.GetManifestResourceStream($"Microsoft.Extensions.AI.Resources.{fileName}");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);

        ms.Position = 0;
        return ms;
    }

    private static ISpeechToTextClient CreateSpeechToTextClient(HttpClient httpClient, string modelId) =>
        new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .AsSpeechToTextClient(modelId);
}
