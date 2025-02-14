// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA2214 // Do not call overridable methods in constructors

namespace Microsoft.Extensions.AI;

public abstract class AudioTranscriptionClientIntegrationTests : IDisposable
{
    private readonly IAudioTranscriptionClient? _client;

    protected AudioTranscriptionClientIntegrationTests()
    {
        _client = CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract IAudioTranscriptionClient? CreateClient();

    [ConditionalFact]
    public virtual async Task TranscribeAsync_SingleAudioRequestMessage()
    {
        SkipIfNotEnabled();

        using var audioStream = GetAudioStream("audio001.wav");
        var response = await _client.TranscribeAsync([audioStream.ToAsyncEnumerable()]);

        Assert.Contains("gym", response.AudioTranscription.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public virtual async Task TranscribeAsync_MultipleAudioRequestMessage()
    {
        SkipIfNotEnabled();

        using var firstAudioStream = GetAudioStream("audio001.wav");
        using var secondAudioStream = GetAudioStream("audio002.wav");

        var response = await _client.TranscribeAsync([firstAudioStream.ToAsyncEnumerable(), secondAudioStream.ToAsyncEnumerable()]);

        var firstFileChoice = Assert.Single(response.Choices.Where(c => c.InputIndex == 0));
        var secondFileChoice = Assert.Single(response.Choices.Where(c => c.InputIndex == 1));

        Assert.Contains("gym", firstFileChoice.Text);
        Assert.Contains("who", secondFileChoice.Text);
    }

    [ConditionalFact]
    public virtual async Task TranscribeStreamingAsync_SingleStreamingResponseChoice()
    {
        SkipIfNotEnabled();

        using var audioStream = GetAudioStream("audio001.wav");

        StringBuilder sb = new();
        await foreach (var chunk in _client.TranscribeStreamingAsync([audioStream.ToAsyncEnumerable()]))
        {
            sb.Append(chunk.Text);
        }

        string responseText = sb.ToString();
        Assert.Contains("finally", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gym", responseText, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public virtual async Task TranscribeStreamingAsync_MultipleStreamingResponseChoice()
    {
        SkipIfNotEnabled();

        using var firstAudioStream = GetAudioStream("audio001.wav");
        using var secondAudioStream = GetAudioStream("audio002.wav");

        StringBuilder firstSb = new();
        StringBuilder secondSb = new();
        await foreach (var chunk in _client.TranscribeStreamingAsync([firstAudioStream.ToAsyncEnumerable(), secondAudioStream.ToAsyncEnumerable()]))
        {
            if (chunk.InputIndex == 0)
            {
                firstSb.Append(chunk.Text);
            }
            else
            {
                secondSb.Append(chunk.Text);
            }
        }

        string firstTranscription = firstSb.ToString();
        Assert.Contains("finally", firstTranscription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gym", firstTranscription, StringComparison.OrdinalIgnoreCase);

        string secondTranscription = secondSb.ToString();
        Assert.Contains("who would", secondTranscription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("go for", secondTranscription, StringComparison.OrdinalIgnoreCase);
    }

    private static Stream GetAudioStream(string fileName)
    {
        using Stream? s = typeof(AudioTranscriptionClientIntegrationTests).Assembly.GetManifestResourceStream($"Microsoft.Extensions.AI.Resources.{fileName}");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);

        ms.Position = 0;
        return ms;
    }

    [MemberNotNull(nameof(_client))]
    protected void SkipIfNotEnabled()
    {
        if (_client is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }
}
