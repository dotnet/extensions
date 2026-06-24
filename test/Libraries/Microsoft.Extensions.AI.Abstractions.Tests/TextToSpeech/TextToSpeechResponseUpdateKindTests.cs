// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechResponseUpdateKindTests
{
    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        Assert.Equal("abc", new TextToSpeechResponseUpdateKind("abc").Value);
    }

    [Fact]
    public void Constructor_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new TextToSpeechResponseUpdateKind(null!));
        Assert.Throws<ArgumentException>("value", () => new TextToSpeechResponseUpdateKind("  "));
    }

    [Fact]
    public void Equality_UsesOrdinalIgnoreCaseComparison()
    {
        var kind1 = new TextToSpeechResponseUpdateKind("abc");
        var kind2 = new TextToSpeechResponseUpdateKind("ABC");
        Assert.True(kind1.Equals(kind2));
        Assert.True(kind1.Equals((object)kind2));
        Assert.True(kind1 == kind2);
        Assert.False(kind1 != kind2);

        var kind3 = new TextToSpeechResponseUpdateKind("def");
        Assert.False(kind1.Equals(kind3));
        Assert.False(kind1.Equals((object)kind3));
        Assert.False(kind1 == kind3);
        Assert.True(kind1 != kind3);

        Assert.Equal(kind1.GetHashCode(), new TextToSpeechResponseUpdateKind("abc").GetHashCode());
        Assert.Equal(kind1.GetHashCode(), new TextToSpeechResponseUpdateKind("ABC").GetHashCode());
    }

    [Fact]
    public void Singletons_UseKnownValues()
    {
        Assert.Equal(TextToSpeechResponseUpdateKind.SessionOpen.ToString(), TextToSpeechResponseUpdateKind.SessionOpen.Value);
        Assert.Equal(TextToSpeechResponseUpdateKind.Error.ToString(), TextToSpeechResponseUpdateKind.Error.Value);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdating.ToString(), TextToSpeechResponseUpdateKind.AudioUpdating.Value);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdated.ToString(), TextToSpeechResponseUpdateKind.AudioUpdated.Value);
        Assert.Equal(TextToSpeechResponseUpdateKind.SessionClose.ToString(), TextToSpeechResponseUpdateKind.SessionClose.Value);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        var kind = new TextToSpeechResponseUpdateKind("abc");
        string json = JsonSerializer.Serialize(kind, TestJsonSerializerContext.Default.TextToSpeechResponseUpdateKind);
        Assert.Equal("\"abc\"", json);

        var result = JsonSerializer.Deserialize<TextToSpeechResponseUpdateKind>(json, TestJsonSerializerContext.Default.TextToSpeechResponseUpdateKind);
        Assert.Equal(kind, result);
    }
}
