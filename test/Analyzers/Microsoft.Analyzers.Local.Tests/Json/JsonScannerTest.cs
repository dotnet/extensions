// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonScannerTest
{
    [Fact]
    public void TestUnexpectedLookahead()
    {
        JsonParseException ex;

        ex = Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("trUe"));
        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(2, ex.Position.Column);

        ex = Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("tr"));
        Assert.Equal(ParsingError.IncompleteMessage, ex.Error);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(2, ex.Position.Column);
    }

    [Fact]
    public void TestIncompleteComment()
    {
        var ex = Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("{ /1 }"));

        Assert.Equal(ParsingError.InvalidOrUnexpectedCharacter, ex.Error);
        Assert.Contains("'1'", ex.Message);
        Assert.Equal(0, ex.Position.Line);
        Assert.Equal(3, ex.Position.Column);

        ex = Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("{ // ignored text }"));

        Assert.Equal(ParsingError.IncompleteMessage, ex.Error);

        ex = Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("{ /* ignored text }"));

        Assert.Equal(ParsingError.IncompleteMessage, ex.Error);
    }

    [Fact]
    public void TestBlockCommentTermination()
    {
        var obj = JsonValue.Parse("{ /* * / */ }");

        Assert.Equal(JsonValueType.Object, obj.Type);
        Assert.Equal(0, obj.AsJsonObject?.Count);
    }
}
