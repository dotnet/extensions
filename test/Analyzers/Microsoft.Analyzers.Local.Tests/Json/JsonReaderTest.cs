// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonReaderTest
{
    [Fact]
    public void TestKeyMatchesPreviousValue()
    {
        var jsonObject = JsonValue.Parse("{ \"x\": \"value\", \"value\": \"value\" }");
        Assert.NotEqual(jsonObject, JsonValue.Null);
        Assert.Equal("value", jsonObject["x"].AsString);
        Assert.Equal("value", jsonObject["value"].AsString);
        Assert.Equal(jsonObject["x"], jsonObject["value"]);
    }

    [Fact]
    public void TestDuplicateKeys()
    {
        Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("{ \"x\": \"value\", \"x\": \"value\" }"));
    }

    [Fact]
    public void TestParse()
    {
        Assert.Equal("true", JsonReader.Parse("true").AsString);

        using (var reader = new StringReader("true"))
        {
            Assert.Equal("true", JsonReader.Parse(reader).AsString);
        }

        Assert.Throws<ArgumentNullException>("source", () => JsonReader.Parse(default(string)!));
        Assert.Throws<ArgumentNullException>("reader", () => JsonReader.Parse(default(TextReader)!));
    }

    [Fact]
    public void TestNumbers()
    {
        Assert.Equal(0, JsonReader.Parse("0").AsInteger);
        Assert.Equal(0, JsonReader.Parse("-0").AsInteger);
        Assert.Equal(-1, JsonReader.Parse("-1").AsInteger);
        Assert.Equal(-1.0, JsonReader.Parse("-1.0").AsNumber);
        Assert.Equal(-1e1, JsonReader.Parse("-1e1").AsNumber);
        Assert.Equal(-1E1, JsonReader.Parse("-1E1").AsNumber);
        Assert.Equal(-1E+1, JsonReader.Parse("-1E+1").AsNumber);
        Assert.Equal(-10E-1, JsonReader.Parse("-10E-1").AsNumber);
    }

    [Fact]
    public void TestEscapeSequences()
    {
        Assert.Equal("\"", JsonReader.Parse("\"\\\"\"").AsString);
        Assert.Equal("\\", JsonReader.Parse("\"\\\\\"").AsString);
        Assert.Equal("/", JsonReader.Parse("\"\\/\"").AsString);
        Assert.Equal("\b", JsonReader.Parse("\"\\b\"").AsString);
        Assert.Equal("\f", JsonReader.Parse("\"\\f\"").AsString);
        Assert.Equal("\n", JsonReader.Parse("\"\\n\"").AsString);
        Assert.Equal("\r", JsonReader.Parse("\"\\r\"").AsString);
        Assert.Equal("\t", JsonReader.Parse("\"\\t\"").AsString);
        Assert.Equal("\u0123\u4567\u89AB\uCDEF", JsonReader.Parse("\"\\u0123\\u4567\\u89AB\\uCDEF\"").AsString);

        var ex = Assert.Throws<JsonParseException>(() => JsonReader.Parse("\"\\x\""));
        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(2, ex.Position.Column);

        ex = Assert.Throws<JsonParseException>(() => JsonReader.Parse("\"\\u11GA\""));
        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(5, ex.Position.Column);

        ex = Assert.Throws<JsonParseException>(() => JsonReader.Parse("\"\r\""));
        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(1, ex.Position.Column);
    }

    [Fact]
    public void TestArrayMissingComma()
    {
        var ex = Assert.ThrowsAny<JsonParseException>(() => JsonReader.Parse("[ 1 2 ]"));
        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(4, ex.Position.Column);
    }
}
