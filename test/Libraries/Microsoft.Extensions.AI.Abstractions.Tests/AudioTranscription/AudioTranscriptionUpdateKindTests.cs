// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionUpdateKindTests
{
    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        Assert.Equal("abc", new AudioTranscriptionResponseUpdateKind("abc").Value);
    }

    [Fact]
    public void Constructor_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new AudioTranscriptionResponseUpdateKind(null!));
        Assert.Throws<ArgumentException>("value", () => new AudioTranscriptionResponseUpdateKind("  "));
    }

    [Fact]
    public void Equality_UsesOrdinalIgnoreCaseComparison()
    {
        var kind1 = new AudioTranscriptionResponseUpdateKind("abc");
        var kind2 = new AudioTranscriptionResponseUpdateKind("ABC");
        Assert.True(kind1.Equals(kind2));
        Assert.True(kind1.Equals((object)kind2));
        Assert.True(kind1 == kind2);
        Assert.False(kind1 != kind2);

        var kind3 = new AudioTranscriptionResponseUpdateKind("def");
        Assert.False(kind1.Equals(kind3));
        Assert.False(kind1.Equals((object)kind3));
        Assert.False(kind1 == kind3);
        Assert.True(kind1 != kind3);

        Assert.Equal(kind1.GetHashCode(), new AudioTranscriptionResponseUpdateKind("abc").GetHashCode());
        Assert.Equal(kind1.GetHashCode(), new AudioTranscriptionResponseUpdateKind("ABC").GetHashCode());
    }

    [Fact]
    public void Singletons_UseKnownValues()
    {
        // These constants are defined in AudioTranscriptionResponseUpdateKind
        Assert.Equal("sessionopen", AudioTranscriptionResponseUpdateKind.SessionOpen.Value);
        Assert.Equal("error", AudioTranscriptionResponseUpdateKind.Error.Value);
        Assert.Equal("transcribing", AudioTranscriptionResponseUpdateKind.Transcribing.Value);
        Assert.Equal("transcribed", AudioTranscriptionResponseUpdateKind.Transcribed.Value);
        Assert.Equal("sessionclose", AudioTranscriptionResponseUpdateKind.SessionClose.Value);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        var kind = new AudioTranscriptionResponseUpdateKind("abc");
        string json = JsonSerializer.Serialize(kind, TestJsonSerializerContext.Default.AudioTranscriptionResponseUpdateKind);
        Assert.Equal("\"abc\"", json);

        var result = JsonSerializer.Deserialize<AudioTranscriptionResponseUpdateKind>(json, TestJsonSerializerContext.Default.AudioTranscriptionResponseUpdateKind);
        Assert.Equal(kind, result);
    }
}
