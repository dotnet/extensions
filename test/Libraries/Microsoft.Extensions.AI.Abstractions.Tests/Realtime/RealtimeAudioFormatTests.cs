// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeAudioFormatTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var format = new RealtimeAudioFormat("audio/pcm", 16000);

        Assert.Equal("audio/pcm", format.Type);
        Assert.Equal(16000, format.SampleRate);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var format = new RealtimeAudioFormat("audio/pcm", 16000)
        {
            Type = "audio/wav",
            SampleRate = 24000,
        };

        Assert.Equal("audio/wav", format.Type);
        Assert.Equal(24000, format.SampleRate);
    }

    [Fact]
    public void SampleRate_CanBeSetToNull()
    {
        var format = new RealtimeAudioFormat("audio/pcm", 16000)
        {
            SampleRate = null,
        };

        Assert.Null(format.SampleRate);
    }
}
