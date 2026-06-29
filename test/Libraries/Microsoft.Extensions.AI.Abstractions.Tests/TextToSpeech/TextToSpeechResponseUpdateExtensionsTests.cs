// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechResponseUpdateExtensionsTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((List<TextToSpeechResponseUpdate>)null!).ToTextToSpeechResponse());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToTextToSpeechResponse_SuccessfullyCreatesResponse(bool useAsync)
    {
        TextToSpeechResponseUpdate[] updates =
        [
            new([new DataContent(new byte[] { 1 }, "audio/mpeg")]) { ModelId = "model123", AdditionalProperties = new() { ["a"] = "b" } },
            new([new DataContent(new byte[] { 2 }, "audio/mpeg")]) { ModelId = "model123" },
            new([new DataContent(new byte[] { 3 }, "audio/mpeg")]) { ModelId = "model123", AdditionalProperties = new() { ["c"] = "d" } },
            new() { ResponseId = "someResponse", ModelId = "model123" },
        ];

        TextToSpeechResponse response = useAsync ?
            await YieldAsync(updates).ToTextToSpeechResponseAsync() :
            updates.ToTextToSpeechResponse();

        Assert.NotNull(response);

        Assert.Equal("someResponse", response.ResponseId);
        Assert.Equal("model123", response.ModelId);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(2, response.AdditionalProperties.Count);
        Assert.Equal("b", response.AdditionalProperties["a"]);
        Assert.Equal("d", response.AdditionalProperties["c"]);

        Assert.Equal(3, response.Contents.Count);
        Assert.All(response.Contents, c => Assert.IsType<DataContent>(c));

        Assert.Null(response.Usage);
    }

    [Fact]
    public async Task ToTextToSpeechResponse_UsageContentExtractedFromContents()
    {
        TextToSpeechResponseUpdate[] updates =
        {
            new() { Contents = [new DataContent(new byte[] { 1 }, "audio/mpeg")] },
            new() { Contents = [new UsageContent(new() { TotalTokenCount = 42 })] },
            new() { Contents = [new DataContent(new byte[] { 2 }, "audio/mpeg")] },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 12, TotalTokenCount = 24 })] },
        };

        TextToSpeechResponse response = await YieldAsync(updates).ToTextToSpeechResponseAsync();

        Assert.NotNull(response);

        Assert.NotNull(response.Usage);
        Assert.Equal(12, response.Usage.InputTokenCount);
        Assert.Equal(66, response.Usage.TotalTokenCount);

        Assert.Equal(2, response.Contents.Count);
        Assert.All(response.Contents, c => Assert.IsType<DataContent>(c));
    }

    private static async IAsyncEnumerable<TextToSpeechResponseUpdate> YieldAsync(IEnumerable<TextToSpeechResponseUpdate> updates)
    {
        foreach (TextToSpeechResponseUpdate update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
