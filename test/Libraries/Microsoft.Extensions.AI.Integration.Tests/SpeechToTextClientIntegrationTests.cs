﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

#pragma warning disable CA2214 // Do not call overridable methods in constructors

namespace Microsoft.Extensions.AI;

public abstract class SpeechToTextClientIntegrationTests : IDisposable
{
    private readonly ISpeechToTextClient? _client;

    protected SpeechToTextClientIntegrationTests()
    {
        _client = CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract ISpeechToTextClient? CreateClient();

    [ConditionalFact]
    public virtual async Task GetTextAsync_SingleAudioRequestMessage()
    {
        SkipIfNotEnabled();

        using var audioSpeechStream = GetAudioStream("audio001.mp3");
        var response = await _client.GetTextAsync(audioSpeechStream);

        Assert.Contains("gym", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public virtual async Task GetStreamingTextAsync_SingleStreamingResponseChoice()
    {
        SkipIfNotEnabled();

        using var audioSpeechStream = GetAudioStream("audio001.mp3");

        StringBuilder sb = new();
        await foreach (var chunk in _client.GetStreamingTextAsync(audioSpeechStream))
        {
            sb.Append(chunk.Text);
        }

        string responseText = sb.ToString();
        Assert.Contains("finally", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gym", responseText, StringComparison.OrdinalIgnoreCase);
    }

    private static Stream GetAudioStream(string fileName)
    {
        using Stream? s = typeof(SpeechToTextClientIntegrationTests).Assembly.GetManifestResourceStream($"Microsoft.Extensions.AI.Resources.{fileName}");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);

        ms.Position = 0;
        return ms;
    }

    [MemberNotNull(nameof(_client))]
    protected void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];

        if (skipIntegration is not null || _client is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }
}
