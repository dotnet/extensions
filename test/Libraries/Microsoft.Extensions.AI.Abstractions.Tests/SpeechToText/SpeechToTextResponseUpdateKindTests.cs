// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextResponseUpdateKindTests
{
    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        Assert.Equal("abc", new SpeechToTextResponseUpdateKind("abc").Value);
    }

    [Fact]
    public void Constructor_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new SpeechToTextResponseUpdateKind(null!));
        Assert.Throws<ArgumentException>("value", () => new SpeechToTextResponseUpdateKind("  "));
    }

    [Fact]
    public void Equality_UsesOrdinalIgnoreCaseComparison()
    {
        var kind1 = new SpeechToTextResponseUpdateKind("abc");
        var kind2 = new SpeechToTextResponseUpdateKind("ABC");
        Assert.True(kind1.Equals(kind2));
        Assert.True(kind1.Equals((object)kind2));
        Assert.True(kind1 == kind2);
        Assert.False(kind1 != kind2);

        var kind3 = new SpeechToTextResponseUpdateKind("def");
        Assert.False(kind1.Equals(kind3));
        Assert.False(kind1.Equals((object)kind3));
        Assert.False(kind1 == kind3);
        Assert.True(kind1 != kind3);

        Assert.Equal(kind1.GetHashCode(), new SpeechToTextResponseUpdateKind("abc").GetHashCode());
        Assert.Equal(kind1.GetHashCode(), new SpeechToTextResponseUpdateKind("ABC").GetHashCode());
    }

    [Fact]
    public void Singletons_UseKnownValues()
    {
        Assert.Equal(SpeechToTextResponseUpdateKind.SessionOpen.ToString(), SpeechToTextResponseUpdateKind.SessionOpen.Value);
        Assert.Equal(SpeechToTextResponseUpdateKind.Error.ToString(), SpeechToTextResponseUpdateKind.Error.Value);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdating.ToString(), SpeechToTextResponseUpdateKind.TextUpdating.Value);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdated.ToString(), SpeechToTextResponseUpdateKind.TextUpdated.Value);
        Assert.Equal(SpeechToTextResponseUpdateKind.SessionClose.ToString(), SpeechToTextResponseUpdateKind.SessionClose.Value);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        var kind = new SpeechToTextResponseUpdateKind("abc");
        string json = JsonSerializer.Serialize(kind, TestJsonSerializerContext.Default.SpeechToTextResponseUpdateKind);
        Assert.Equal("\"abc\"", json);

        var result = JsonSerializer.Deserialize<SpeechToTextResponseUpdateKind>(json, TestJsonSerializerContext.Default.SpeechToTextResponseUpdateKind);
        Assert.Equal(kind, result);
    }
}
